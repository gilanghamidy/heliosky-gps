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
            serialPort.BaudRate = 115200;
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
                uint bytesRead = await reader.LoadAsync(BufferLength);

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

                        // Transmit the queue
                        while(transmitQueue.Count != 0)
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
                        CancellationTokenSource timeoutToken = new CancellationTokenSource(serialPort.ReadTimeout);
                        var loadingTask = dataReader.LoadAsync(BufferLength).AsTask(timeoutToken.Token);
                        

                        while(dataReader.UnconsumedBufferLength != 0)
                        {
                            // Consume buffer while waiting for data
                            byte currentByte = dataReader.ReadByte();
                            bool fail = false;

                            // State machine
                            switch(currentState)
                            {
                                case 0: // Start with Header 1
                                    if(currentByte == UBX.UBXModelBase.Header1)
                                    {
                                        currentlyProcessed.Enqueue(currentByte);
                                        currentState++;
                                    }
                                    else
                                    {
                                        fail = true;
                                    }
                                    break;
                                case 1: // Followed by Header 2, otherwise fail
                                    if (currentByte == UBX.UBXModelBase.Header2)
                                    {
                                        currentlyProcessed.Enqueue(currentByte);
                                        currentState++;
                                    }
                                    else
                                    {
                                        fail = true;
                                    }
                                    break;
                                case 2: // Followed by Class ID
                                    currentlyProcessed.Enqueue(currentByte);
                                    currentState++;
                                    break;
                                case 3: // Followed by Message ID
                                    goto case 2;
                                case 4: // Retrieve Size
                                    payloadLength = currentByte;
                                    goto case 2;
                                case 5: // Continue retrieve size
                                    payloadLength |= ((ushort)(currentByte << 8)); // Second byte of payload length
                                    goto case 2;
                                case 6: // Retrieve the payload
                                    if(payloadLength > 0)
                                    {
                                        currentlyProcessed.Enqueue(currentByte);
                                        payloadLength--;
                                    }
                                    else
                                    {
                                        // First checksum
                                        currentlyProcessed.Enqueue(currentByte);
                                        currentState++;
                                    }
                                    break;
                                case 7: // First checksum
                                    goto case 2;

                            }

#if DEBUG
                            Debug.Write(currentByte.ToString("X") + ' ');
#endif

                            // Reset processing if it encounter invalid header
                            if (fail)
                            {
                                currentState = 0;
                                currentlyProcessed.Clear();
#if DEBUG
                                Debug.WriteLine("");
#endif
                            }

                            if (currentState == 8)
                            {
                                try
                                {
                                    Debug.WriteLine("Package received: " + currentlyProcessed.Count + " bytes");
                                    var arr = currentlyProcessed.ToArray();
                                    DebugHelper.PrintArray(arr);
                                    var package = UBX.UBXModelBase.TryParse(arr);
                                    Debug.WriteLine(package.ToString());
                                }
                                catch(UBX.UBXException ex)
                                {
#if DEBUG
                                    Debug.WriteLine("Failed parsing UBX package: " + ex);                               
#endif
                                }
                                catch(Exception ex)
                                { 
#if DEBUG
                                    Debug.WriteLine("Exception occured during parsing: " + ex);
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
                        catch(TaskCanceledException)
                        {

                        }
#if DEBUG
                        Debug.WriteLine("#");
#endif
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
    }
}
