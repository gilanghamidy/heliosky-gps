using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
            9600,
            19200,
            38400,
            57600,
            115200
        };

        /// <summary>
        /// Private class which manages the list of expected message from GPS serial device
        /// </summary>
        private class ExpectingList
        {
            private Dictionary<ExpectingDescription, ExpectingContext> expectingList = new Dictionary<ExpectingDescription, ExpectingContext>();

            public async Task<T> ExpectAsync<T>() where T : UBX.UBXModelBase
            {
                var expectingDesc = new ExpectingDescription(typeof(T));

                if (expectingList.ContainsKey(expectingDesc))
                    throw new InvalidOperationException(String.Format("Already expecting type {0}", typeof(T).FullName));

                
                var expectingContext = new ExpectingContext();

                expectingList.Add(expectingDesc, expectingContext);

                await expectingContext.WaitForResponse();

                expectingList.Remove(expectingDesc);

                return (T)expectingContext.ResponseReceived;
            }

            public async Task<bool> ExpectAcknowledgeAsync(byte classId, byte messageId)
            {
                var expectingDesc = new ExpectingDescription(classId, messageId);

                if (expectingList.ContainsKey(expectingDesc))
                    throw new InvalidOperationException(String.Format("Already expecting ACK for Class {0}, MessageID {1}", classId, messageId));

                var expectingContext = new ExpectingContext();
                expectingList.Add(expectingDesc, expectingContext);
                await expectingContext.WaitForResponse();
                expectingList.Remove(expectingDesc);

                var retVal = expectingContext.ResponseReceived;

                if (retVal is UBX.Acknowledge)
                    return true;
                else
                    return false;
            }

            public void AbortExpectAsync<T>()
            {
                var expectingDesc = new ExpectingDescription(typeof(T));
                if (!expectingList.ContainsKey(expectingDesc))
                    return;

                var expectingContext = expectingList[expectingDesc];
                expectingList.Remove(expectingDesc);
                expectingContext.Cancel();
            }

            public void NotifyIfExpected(UBX.UBXModelBase obj)
            {
                var expectingDesc = new ExpectingDescription(obj);
                try
                {
                    var expectingContext = expectingList[expectingDesc];
                    expectingContext.NotifyResponseReceived(obj);
                }
                catch(KeyNotFoundException)
                {
                    return;
                }
            }
        }

        private class ExpectingDescription
        {
            public enum ExpectingMode
            {
                Regular,
                Acknowledge
            }

            private Type expectingType;
            private byte classId, messageId;
            private ExpectingMode expectedMessageMode;

            public ExpectingDescription(UBX.UBXModelBase message)
            {
                if (message is UBX.AcknowledgeBase)
                {
                    var ack = message as UBX.AcknowledgeBase;
                    this.classId = ack.ClassID;
                    this.messageId = ack.MessageID;
                    this.expectedMessageMode = ExpectingMode.Acknowledge;
                }
                else
                {
                    this.expectingType = message.GetType();
                    this.expectedMessageMode = ExpectingMode.Regular;
                    
                }
            }

            public ExpectingDescription(Type messageType)
            {
                this.expectingType = messageType;
                this.expectedMessageMode = ExpectingMode.Regular;
            }

            public ExpectingDescription(byte classId, byte messageId)
            {
                this.classId = classId;
                this.messageId = messageId;
                this.expectedMessageMode = ExpectingMode.Acknowledge;
            }

            public override bool Equals(object obj)
            {
                if(base.Equals(obj))
                    return true;
                else
                {
                    ExpectingDescription desc = obj as ExpectingDescription;
                    if(desc != null)
                    {
                        if(this.expectedMessageMode == desc.expectedMessageMode)
                        {
                            if (this.expectedMessageMode == ExpectingMode.Regular)
                                return this.expectingType.Equals(desc.expectingType);
                            else
                                return this.classId == desc.classId && this.messageId == desc.messageId;
                        }
                    }
                }
                return false;
            }

            public override int GetHashCode()
            {
                if (expectedMessageMode == ExpectingMode.Regular)
                    return expectedMessageMode.GetHashCode();
                else
                    return 123 * classId * messageId;
            }
        }

        private class ExpectingContext
        {
            private CancellationTokenSource notifyTokenSource;
            
            public ExpectingContext()
            {
                notifyTokenSource = new CancellationTokenSource();
            }

            public UBX.UBXModelBase ResponseReceived { get; private set; }
            
            public async Task WaitForResponse()
            {
                try
                {
                    await Task.Delay(-1, notifyTokenSource.Token);
                }
                catch(TaskCanceledException)
                {

                }
            }

            public void NotifyResponseReceived(UBX.UBXModelBase obj)
            {
                this.ResponseReceived = obj;
                notifyTokenSource.Cancel();
            }

            public void Cancel()
            {
                notifyTokenSource.Cancel();
            }
        }

        

        public const uint BufferLength = 10240;

        private DeviceInformation deviceInfo;
        private CancellationTokenSource cancelToken;

        private UBX.ConfigPort portConfig;

        private Queue<UBX.UBXModelBase> transmitQueue = new Queue<UBX.UBXModelBase>();
        private Dictionary<UBX.UBXModelBase, CancellationTokenSource> transmissionNotification = new Dictionary<UBX.UBXModelBase, CancellationTokenSource>();

        private ExpectingList expectingList = new ExpectingList();
        
        private uint baudRate;
        private Task runningListenTask;

        bool running = false;

        public UBXSerialGPS(DeviceInformation deviceInfo, UBX.ConfigPort portConfig)
        {
            this.deviceInfo = deviceInfo;
            this.portConfig = portConfig;
            this.baudRate = AvailableBaudRate[1]; // Default BaudRate
        }

        private static async Task<SerialDevice> InitializeSerialPort(DeviceInformation info, uint baudRate)
        {
            var retSerialPort = await SerialDevice.FromIdAsync(info.Id);

            retSerialPort.Parity = SerialParity.None;
            retSerialPort.StopBits = SerialStopBitCount.One;
            retSerialPort.DataBits = 8;
            retSerialPort.BaudRate = baudRate;
            retSerialPort.Handshake = SerialHandshake.None;

            return retSerialPort;
        }

        public async Task Start()
        {
            // Autodetect current baud rate
            await AutodetectPortSpeed();
            //this.baudRate = 115200;

            // Config the port
            await TryConfigPort();

            this.runningListenTask = Listen(this.baudRate);
        }

        private async Task AutodetectPortSpeed()
        {
            foreach(uint currentBaud in AvailableBaudRate)
            {
#if DEBUG
                Debug.WriteLine(String.Format("Probing speed: {0}", currentBaud));
#endif

                var success = await ProbePortSpeed(currentBaud);
                if(success)
                {
                    this.baudRate = currentBaud;
#if DEBUG
                    Debug.WriteLine(String.Format("Success probing speed: {0}", currentBaud));
#endif
                    break;
                }

#if DEBUG
                Debug.WriteLine(String.Format("Failed probing speed: {0}", currentBaud));
#endif
            }
        }

        private async Task TryConfigPort()
        {
            if (portConfig == null)
                return;

            try
            {
                this.runningListenTask = Listen(this.baudRate);

                await this.WriteConfig(portConfig);

                // Assume that the baud rate is changed
                this.baudRate = portConfig.BaudRate;
#if DEBUG
                Debug.WriteLine("Success config BaudRate to: {0}", this.baudRate);
#endif
                
            }
            finally
            {
                await Stop();
            }
        }

        private async Task<bool> ProbePortSpeed(uint testingBaudRate)
        {
            try
            {
                this.runningListenTask = Listen(testingBaudRate);

                await Task.Delay(2000);

                var task = this.PollMessageAsync<UBX.NavigationClock>();
                
                if(await Task.WhenAny(task, Task.Delay(10000)) == task)
                {
                    var result = await task;

                    if (result == null)
                        return false;
                    else
                        return true;
                }
                else
                {
                    this.AbortPollMessageAsync<UBX.NavigationClock>();
                    return false;
                }
            }
            finally
            {
                await Stop();
            }
        }

        public async Task Stop()
        {
            if (this.runningListenTask == null)
                return;

            cancelToken.Cancel();

            try
            {
                // Wait indefinitely until the listening loop has shutted down completely
                await runningListenTask;
            }
            catch(TaskCanceledException)
            {

            }
            finally
            {
                runningListenTask = null;
            }

        }

        private async Task Listen(uint baudRate)
        {
            if (running)
                return;

            running = true;
            DataReader dataReader = null;
            DataWriter dataWriter = null;
            cancelToken = new CancellationTokenSource();
            SerialDevice serialPort = null;

#if DEBUG
            Debug.WriteLine("Listening start...");
#endif

            try
            {
                serialPort = await InitializeSerialPort(this.deviceInfo, baudRate);

                serialPort.WriteTimeout = TimeSpan.FromMilliseconds(100);
                serialPort.ReadTimeout = TimeSpan.FromMilliseconds(100);
                    
                dataReader = new DataReader(serialPort.InputStream);
                dataReader.InputStreamOptions = InputStreamOptions.Partial;

                dataWriter = new DataWriter(serialPort.OutputStream);

                Queue<byte> currentlyProcessed = new Queue<byte>(1024);
                byte currentState = 0;
                ushort payloadLength = 0;

                while(true)
                {
                    cancelToken.Token.ThrowIfCancellationRequested();

                    await WriteQueuedMessages(dataWriter);

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
                                expectingList.NotifyIfExpected(package);
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
                    catch(Exception ex)
                    {
#if DEBUG
                        Debug.WriteLine("Unexpected Exception when awaiting loading: " + ex);
#endif
                    }
                }
            }
            catch (OperationCanceledException)
            {
#if DEBUG
                Debug.WriteLine("Listening port stopped!");
#endif
            }
            catch(Exception ex)
            {
#if DEBUG
                Debug.WriteLine("Unexpected Exception happen during the loop: " + ex);
#endif
            }
            finally
            {
                if (dataReader != null)
                {
                    dataReader.DetachStream();
                    dataReader.Dispose();
                    dataReader = null;
                }

                if(serialPort != null)
                {
                    serialPort.Dispose();
                    serialPort = null;
                }

                running = false;
                cancelToken = null;
            }
        }

        private async Task WriteQueuedMessages(DataWriter writer)
        {
            // Transmit the queue
            while (transmitQueue.Count != 0)
            {
                var currentTransmission = transmitQueue.Dequeue();
#if DEBUG
                Debug.WriteLine("Transmitting package start: " + currentTransmission.ToString());
#endif
                uint bytes = await WriteData(writer, currentTransmission.ToBinaryData());

                if(this.transmissionNotification.ContainsKey(currentTransmission))
                {
                    transmissionNotification[currentTransmission].Cancel();
                }
#if DEBUG
                Debug.WriteLine("Transmitting package completed: " + bytes + " bytes written");
#endif
            }
        }

        public void TransmitMessage(UBX.UBXModelBase messageToTransmit)
        {
            transmitQueue.Enqueue(messageToTransmit);
        }

        private static async Task<uint> WriteData(DataWriter writer, byte[] b)
        {
            try
            {
                writer.WriteBytes(b);

                uint bytesWritten = await writer.StoreAsync();
                return bytesWritten;
            }
            finally
            {

            }
        }

        public async Task<bool> WriteConfigAsync(UBX.UBXModelBase data)
        {
            if (!UBX.UBXModelBase.IsConfigMessage(data))
                throw new NotSupportedException("WriteConfig only available for config type UBX message");

            TransmitMessage(data);
            var attr = UBX.UBXModelBase.GetMessageAttribute(data);
            return await expectingList.ExpectAcknowledgeAsync(attr.ClassID, attr.MessageID);
        }

        public async Task WriteConfig(UBX.UBXModelBase data)
        {
            if (!UBX.UBXModelBase.IsConfigMessage(data))
                throw new NotSupportedException("WriteConfig only available for config type UBX message");

            var notifyToken = new CancellationTokenSource();

            // Register to wait for transmission
            this.transmissionNotification.Add(data, notifyToken);

            // Put in queue
            TransmitMessage(data);

            // Wait until the message is transmitted
            try
            {
                await Task.Delay(-1, notifyToken.Token);
            }
            catch (TaskCanceledException)
            {

            }

            // Remove from notification list
            this.transmissionNotification.Remove(data);

            
        }

        public async Task<T> PollMessageAsync<T>() where T : UBX.UBXModelBase
        {
            var pollMessage = UBX.UBXModelBase.GetPollMessage<T>();

            // Send the data
            TransmitMessage(pollMessage);

            // Wait until expected response comes
            return await expectingList.ExpectAsync<T>();
        }

        public void AbortPollMessageAsync<T>() where T : UBX.UBXModelBase
        {
            expectingList.AbortExpectAsync<T>();
        }
    }
}
