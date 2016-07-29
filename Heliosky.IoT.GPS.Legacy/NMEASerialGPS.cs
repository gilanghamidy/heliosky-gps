/*   NMEASerialGPS.cs
 *   Copyright (C) 2016 Gilang M. Hamidy (gilang.hamidy@gmail.com)
 *   
 *   This file is part of Heliosky.IoT.GPS.Legacy
 * 
 *   Heliosky.IoT.GPS.Legacy is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU Lesser General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   Heliosky.IoT.GPS.Legacy is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU Lesser General Public License for more details.
 *
 *   You should have received a copy of the GNU Lesser General Public License
 *   along with Heliosky.IoT.GPS.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using System.IO;
using System.Runtime.InteropServices;

namespace Heliosky.IoT.GPS.Legacy
{

    [Flags]
    public enum UARTModeFlag : uint
    {

    }

    [Flags]
    public enum IOInProtocolMask : ushort
    {

    }

    [Flags]
    public enum IOOutProtocolMask : ushort
    {

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ConfigUARTPort
    {
        public byte PortID;
        public byte Reserved;
        public ushort TxReady;
        public uint Mode;
        public uint BaudRate;
        public IOInProtocolMask InProtoMask;
        public IOOutProtocolMask OutProtoMask;
        public ushort Reserved4;
        public ushort Reserved5;

        public const uint DefaultMode = 0x8D0;
    }


    public class FixDataReceivedEventArgs : EventArgs
    {
        public FixData Data { get; set; }
    }

    public class NMEASerialGPS
    {
        private DeviceInformation deviceInfo;
        private SerialDevice serialPort;
        private CancellationTokenSource cancelToken;

        private Task backgroundProcess;
        private NMEAParser parser;

        public event EventHandler<FixDataReceivedEventArgs> FixDataReceived;

        bool running;

        //private Thread driverThread;

        public NMEASerialGPS(DeviceInformation deviceInfo)
        {
            this.deviceInfo = deviceInfo;
            running = false;
            parser = new NMEAParser();
        }

        public ConfigUARTPort? UARTConfiguration { get; set; }

        public async void Start()
        {
            try
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

                var schedulerForUiContext = TaskScheduler.FromCurrentSynchronizationContext();
                
                running = true;

                ConfigPort();
                

                backgroundProcess = Task.Factory.StartNew(delegate { SerialListen(schedulerForUiContext); }, TaskCreationOptions.LongRunning, cancelToken.Token);
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        private async void ConfigPort()
        {
            /*
            if(UARTConfiguration.HasValue)
            {
                var config = UARTConfiguration.Value;

                int size = Marshal.SizeOf(config);
                byte[] payload = new byte[size];
                
                IntPtr ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(config, ptr, true);
                unsafe
                {
                    byte* ptrArr = (byte*)ptr.ToPointer();

                    for(int i = 0; i < size; i++)
                    {
                        payload[i] = ptrArr[i];
                    }

                }
                Marshal.FreeHGlobal(ptr);

                DataWriter dataWriter = null;

                try
                {
                    dataWriter = new DataWriter(serialPort.OutputStream);
                    dataWriter.WriteByte(0xB5);  // Sync Char 1
                    dataWriter.WriteByte(0x62); // Sync Char 2
                    dataWriter.WriteByte(0x06); // Message Class CFG
                    dataWriter.WriteByte(0x00); // Message ID PRT
                    dataWriter.WriteUInt16((ushort)size); // Message Size
                    dataWriter.WriteBytes(payload); // Payload
                    var checksum = GetChecksum(payload);
                    dataWriter.WriteByte(checksum.Item1);
                    dataWriter.WriteByte(checksum.Item2);

                    var task = dataWriter.StoreAsync().AsTask();
                    uint bytesWritter = await task;
                }
                finally
                {
                    if (dataWriter != null)
                    {
                        dataWriter.DetachStream();
                        dataWriter.Dispose();
                    }
                        
                }
            }*/
        }

        private Tuple<byte,byte> GetChecksum(byte[] payload)
        {
            byte valA = 0;
            byte valB = 0;

            foreach(byte data in ChecksumEnumerator(payload))
            {
                valA = (byte)((valA + data) % 255);
                valB = (byte)((valB + valA) % 255);
            }

            return new Tuple<byte, byte>(valA, valB);
        }

        private IEnumerable<byte> ChecksumEnumerator(byte[] payload)
        {
            yield return 0x06;
            yield return 0x00;
            yield return (byte)payload.Length;
            yield return (byte)(payload.Length >> 8);
            for (int i = 0; i < payload.Length; i++)
                yield return payload[i];
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
            catch(Exception ex)
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
