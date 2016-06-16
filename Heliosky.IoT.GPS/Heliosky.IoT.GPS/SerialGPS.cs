using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;

using Windows.ApplicationModel.Background;
using System.IO;

namespace Heliosky.IoT.GPS
{
    public class FixDataReceivedEventArgs : EventArgs
    {
        public FixData Data { get; set; }
    }

    public class SerialGPS
    {
        private DeviceInformation deviceInfo;
        private SerialDevice serialPort;
        private CancellationTokenSource cancelToken;

        private Task backgroundProcess;
        private NMEAParser parser;

        public event EventHandler<FixDataReceivedEventArgs> FixDataReceived;

        bool running;

        //private Thread driverThread;

        public SerialGPS(DeviceInformation deviceInfo)
        {
            this.deviceInfo = deviceInfo;
            running = false;
            parser = new NMEAParser();
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

                var schedulerForUiContext = TaskScheduler.FromCurrentSynchronizationContext();
                
                running = true;
                
                backgroundProcess = Task.Factory.StartNew(delegate { SerialListen(schedulerForUiContext); }, TaskCreationOptions.LongRunning, cancelToken.Token);
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

        private async void SerialListen(TaskScheduler uiThreadScheduler)
        {
            StreamReader dataReader = null;

            try
            {
                if(running)
                {
                    dataReader = new StreamReader(serialPort.InputStream.AsStreamForRead());
                    //dataReader.InputStreamOptions = InputStreamOptions.Partial;
                    //uint bufferLen = 1024;

                    while (true)
                    {
                        Task<string> loadAsyncTask;
                        cancelToken.Token.ThrowIfCancellationRequested();
                        loadAsyncTask = dataReader.ReadLineAsync();

                        string bytesRead = await loadAsyncTask;

                        if(bytesRead != null && bytesRead.Length != 0)
                        {
                            try
                            {
                                var gpsData = parser.Parse(bytesRead) as FixData;
                                if (gpsData != null)
                                {
                                    Task.Factory.StartNew(delegate
                                    {
                                        FixDataReceived(this, new FixDataReceivedEventArgs() { Data = gpsData });
                                    }, Task.Factory.CancellationToken, TaskCreationOptions.None, uiThreadScheduler);

                                }
                            }
                            catch(Exception ex)
                            {

                            }
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
