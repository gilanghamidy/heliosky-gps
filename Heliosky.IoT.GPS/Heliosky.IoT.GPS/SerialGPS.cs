using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;

namespace Heliosky.IoT.GPS
{
    public class SerialGPS
    {
        private DeviceInformation deviceInfo;
        private SerialDevice serialPort;
        private CancellationTokenSource cancelToken;

        bool running;

        //private Thread driverThread;

        public SerialGPS(DeviceInformation deviceInfo)
        {
            this.deviceInfo = deviceInfo;
            running = false;
        }

        public async void Start()
        {
            try
            {
                serialPort = await SerialDevice.FromIdAsync(deviceInfo.Id);

                serialPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                serialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                serialPort.BaudRate = 9600;
                serialPort.Parity = SerialParity.None;
                serialPort.StopBits = SerialStopBitCount.One;
                serialPort.DataBits = 8;
                serialPort.Handshake = SerialHandshake.None;

                cancelToken = new CancellationTokenSource();

                running = true;

                SerialListen();

            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        public void Stop()
        {
            try
            {
                if (cancelToken != null)
                {
                    if (!cancelToken.IsCancellationRequested)
                        cancelToken.Cancel();
                }
            }
            finally
            {
                
            }
        }

        private async void SerialListen()
        {
            DataReader dataReader = null;
            

            try
            {
                if(running)
                {
                    dataReader = new DataReader(serialPort.InputStream);
                    dataReader.InputStreamOptions = InputStreamOptions.Partial;
                    uint bufferLen = 1024;

                    while (true)
                    {
                        Task<uint> loadAsyncTask;
                        cancelToken.Token.ThrowIfCancellationRequested();
                        loadAsyncTask = dataReader.LoadAsync(bufferLen).AsTask(cancelToken.Token);




                        uint bytesRead = await loadAsyncTask;

                        if(bytesRead > 0)
                        {

                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
            }
            finally
            {
                if(dataReader != null)
                {
                    dataReader.DetachStream();
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
    }
}
