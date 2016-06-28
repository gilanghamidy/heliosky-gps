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

            serialPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);
            serialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);
            serialPort.BaudRate = 115200;
            serialPort.Parity = SerialParity.None;
            serialPort.StopBits = SerialStopBitCount.One;
            serialPort.DataBits = 8;
            serialPort.Handshake = SerialHandshake.None;

            cancelToken = new CancellationTokenSource();

            // Config the port
            await TryConfigPort();

            running = true;

            Listen();
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

        private async void Listen()
        {
            DataReader dataReader = null;

            try
            {
                if(running)
                {
                    dataReader = new DataReader(serialPort.InputStream);
                    Queue<byte> pendingData = new Queue<byte>();

                    while(true)
                    {
                        cancelToken.Token.ThrowIfCancellationRequested();

                        var loadingTask = dataReader.LoadAsync(BufferLength);

                        while(dataReader.UnconsumedBufferLength != 0)
                        {
                            // Consume buffer while waiting for data

                        }

                        uint readedBuffer = await loadingTask;
                        

                    }
                }
            }
            catch (TaskCanceledException)
            {

            }
            catch (Exception ex)
            {

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
