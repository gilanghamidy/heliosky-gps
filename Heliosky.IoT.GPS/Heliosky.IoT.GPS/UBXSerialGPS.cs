using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;

namespace Heliosky.IoT.GPS
{
    public class UBXSerialGPS
    {
        public static readonly uint[] AvailableBaudRate =
        {
            4800,
            9600,
            19200,
            38400,
            57600,
            115200
        };

        public const uint BufferLength = 10240;

        private DeviceInformation deviceInfo;
        private SerialDevice serialPort;
        private CancellationTokenSource cancelToken;
        private UBX.ConfigPort portConfig;

        private Queue<UBX.UBXModelBase> transmitQueue = new Queue<UBX.UBXModelBase>();
        

        private uint baudRate;

        bool running = false;

        public UBXSerialGPS(DeviceInformation deviceInfo, UBX.ConfigPort portConfig)
        {
            this.deviceInfo = deviceInfo;
            this.portConfig = portConfig;
            this.baudRate = AvailableBaudRate[1]; // Default BaudRate
        }

        public async Task Start()
        {
            serialPort = await SerialDevice.FromIdAsync(deviceInfo.Id);

            serialPort.WriteTimeout = TimeSpan.FromMilliseconds(100);
            serialPort.ReadTimeout = TimeSpan.FromMilliseconds(100);
            serialPort.BaudRate = 9600;
            serialPort.Parity = SerialParity.None;
            serialPort.StopBits = SerialStopBitCount.One;
            serialPort.DataBits = 8;
            serialPort.Handshake = SerialHandshake.None;

            

            cancelToken = new CancellationTokenSource();

            // Config the port
            await TryConfigPort();

            running = true;
        }

        private async Task TryConfigPort()
        {
            if (portConfig == null)
                return;

            // Write configuration
            var configData = portConfig.ToBinaryData();
            await WriteData(configData);

            // Do some delay
            await Task.Delay(2000);

            // Restart the port speed if it was changed
            if(portConfig.BaudRate != baudRate)
            {
                bool success = false;
                uint targetBaud = portConfig.BaudRate;
                do
                {
#if DEBUG
                    Debug.WriteLine(String.Format("Probing speed: {0}", targetBaud));
#endif

                    success = await ProbePortSpeed(targetBaud);

                    if(!success)
                    {
#if DEBUG
                        Debug.WriteLine(String.Format("Failed probing speed: {0}", targetBaud));
#endif

                        var nextItem = (from baud in AvailableBaudRate
                                        where baud < targetBaud
                                        orderby baud descending
                                        select baud).FirstOrDefault();

                        if(nextItem == 0)
                        {
                            break;
                        }
                        else
                        {
                            targetBaud = nextItem;
                        }
                    }
                }
                while (!success);

                if(success)
                {
#if DEBUG
                    Debug.WriteLine(String.Format("Success probing speed: {0}", targetBaud));
#endif
                    baudRate = targetBaud;
                    serialPort.BaudRate = targetBaud;
                }
                else
                {
#if DEBUG
                    Debug.WriteLine("Fallback to speed 9600");
#endif
                    baudRate = AvailableBaudRate[1];
                    serialPort.BaudRate = baudRate;
                }
            }
        }

        private async Task<bool> ProbePortSpeed(uint baudRate)
        {
            DataReader reader = null;

            try
            {
                serialPort.BaudRate = baudRate;

                reader = new DataReader(serialPort.InputStream);

                var cancelToken = new CancellationTokenSource(1500);
                uint bytesRead = await reader.LoadAsync(BufferLength).AsTask(cancelToken.Token);

                if (bytesRead == 0)
                    return false;
                else
                {
#if DEBUG
                    var buf = new byte[bytesRead];
                    reader.ReadBytes(buf);
                    DebugHelper.PrintArray(buf);
#endif

                    return true;
                }
                    
            }
            catch(TaskCanceledException)
            {
                // Timeout
                return false;
            }
            finally
            {
                if(reader != null)
                {
                    reader.DetachStream();
                    reader.Dispose();
                    reader = null;
                }
            }
        }

        public async void Listen()
        {
            DataReader dataReader = null;
#if DEBUG
            Debug.WriteLine("Listening start...");
#endif
            try
            {
                if(running)
                {
                    serialPort.WriteTimeout = TimeSpan.FromMilliseconds(100);
                    serialPort.ReadTimeout = TimeSpan.FromMilliseconds(100);
                    
                    dataReader = new DataReader(serialPort.InputStream);
                    dataReader.InputStreamOptions = InputStreamOptions.Partial;

                    Queue<byte> currentlyProcessed = new Queue<byte>(1024);
                    byte currentState = 0;
                    ushort payloadLength = 0;

                    while(true)
                    {
                        cancelToken.Token.ThrowIfCancellationRequested();

                        await TransmitMessage();

                        // Longer timeout to prevent package drop
                        CancellationTokenSource timeoutToken = new CancellationTokenSource(serialPort.ReadTimeout.Milliseconds * 10);
                        var loadingTask = dataReader.LoadAsync(BufferLength).AsTask(timeoutToken.Token);


                        while (dataReader.UnconsumedBufferLength != 0)
                        {
                            // Consume buffer while waiting for data
                            byte currentByte = dataReader.ReadByte();
                            bool fail = false;

                            currentlyProcessed.Enqueue(currentByte);

                            // State machine:
                            // 0: Header 1
                            // 1: Header 2
                            // 2: Class ID
                            // 3: Message ID
                            // 4: Least significant byte of size
                            // 5: Most significant byte of size
                            // 6: Payload with 1st byte of checksum
                            // 7: 2nd byte of checksum
                            // 8: Processing
                            switch (currentState)
                            {
                                case 0: // Start with Header 1
                                    if (currentByte != UBX.UBXModelBase.Header1)
                                        fail = true;
                                    break;
                                case 1: // Followed by Header 2, otherwise fail
                                    if (currentByte != UBX.UBXModelBase.Header2)
                                        fail = true;
                                    break;
                                case 4: // Retrieve Size
                                    payloadLength = currentByte;
                                    break;
                                case 5: // Continue retrieve size
                                    payloadLength |= ((ushort)(currentByte << 8)); // Second byte of payload length
                                    break;
                            }

#if DEBUG && VERBOSE
                            Debug.Write(currentByte.ToString("X") + ' ');
#endif
                            
                            // Reset processing if it encounter invalid header
                            if (fail)
                            {
                                currentState = 0;
                                currentlyProcessed.Clear();
#if DEBUG && VERBOSE
                                Debug.WriteLine("");
#endif
                            }
                            else if(currentState != 6)
                            {
                                // Increment state
                                currentState++;
                            }
                            else if(currentState == 6)
                            {
                                // Loading the payload
                                if (payloadLength > 0)
                                    payloadLength--;
                                else
                                    currentState++;
                            }

                            if (currentState == 8)
                            {
                                try
                                {
                                    var arr = currentlyProcessed.ToArray();

#if DEBUG
                                    Debug.WriteLine("Package received: " + currentlyProcessed.Count + " bytes");
#if VERBOSE
                                    DebugHelper.PrintArray(arr);
#endif
#endif
                                    var package = UBX.UBXModelBase.TryParse(arr);

#if DEBUG
                                    Debug.WriteLine(package.ToString());
#endif
                                }
                                catch (UBX.UBXException ex)
                                {
#if DEBUG
                                    Debug.WriteLine("Failed parsing UBX package: " + ex);
#endif
                                }
                                catch (Exception ex)
                                {
#if DEBUG
                                    Debug.WriteLine("Exception occured during parsing: " + ex, ex);
#endif               
                                }
                                finally
                                {
                                    currentlyProcessed.Clear();
                                    currentState = 0;
                                }

                            }
                        }

                        try
                        {
                            uint readedBuffer = await loadingTask;
                        }
                        catch (TaskCanceledException)
                        {

                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
#if DEBUG
                Debug.WriteLine("Listening port stopped!");
#endif
            }
            finally
            {
                if (dataReader != null)
                {
                    dataReader.Dispose();
                    dataReader = null;
                }

                if (serialPort != null)
                {
                    serialPort.Dispose();
                    serialPort = null;
                }

                running = false;
            }
        }

        private async Task TransmitMessage()
        {
            // Transmit the queue
            while (transmitQueue.Count != 0)
            {
                var currentTransmission = transmitQueue.Dequeue();
#if DEBUG
                Debug.WriteLine("Transmitting package start: " + currentTransmission.ToString());
#endif
                await WriteData(currentTransmission.ToBinaryData());
#if DEBUG
                Debug.WriteLine("Transmitting package completed");
#endif
            }
        }

        public void TransmitMessage(UBX.UBXModelBase messageToTransmit)
        {
            transmitQueue.Enqueue(messageToTransmit);
        }

        private async Task<uint> WriteData(byte[] b)
        {
            if (serialPort == null)
                throw new InvalidOperationException("Serial port is not initialized");

            DataWriter writer = null;

            try
            {
                writer = new DataWriter(serialPort.OutputStream);

                writer.WriteBytes(b);

                uint bytesWritten = await writer.StoreAsync();
                return bytesWritten;
            }
            finally
            {
                if(writer != null)
                {
                    writer.DetachStream();
                    writer.Dispose();
                    writer = null;
                }
            }
        }

        private async Task<T> Expect<T>() where T : UBX.UBXModelBase
        {
            return null;
        }

        public async Task<T> PollMessage<T>() where T : UBX.UBXModelBase
        {
            byte[] pollMessage = UBX.UBXModelBase.GetPollMessage<T>();

            return null;
        }
    }
}
