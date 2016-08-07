/*   UBXSerialGPS.cs
 *   Copyright (C) 2016 Gilang M. Hamidy (gilang.hamidy@gmail.com)
 *   
 *   This file is part of Heliosky.IoT.GPS
 * 
 *   Heliosky.IoT.GPS is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU Lesser General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   Heliosky.IoT.GPS is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU Lesser General Public License for more details.
 *
 *   You should have received a copy of the GNU Lesser General Public License
 *   along with Heliosky.IoT.GPS.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            /// <summary>
            /// Expect a message of specific type from GPS serial device.
            /// </summary>
            /// <typeparam name="T">Type of message which is expected</typeparam>
            /// <returns>The message which is expected</returns>
            /// <remarks>
            /// This method is performed asynchronously and it should be awaited. The method will complete 
            /// when it receive the <u>first message</u> with required type or if it is aborted by calling
            /// <see cref="AbortExpectAsync{T}"/>.
            /// </remarks>
            public async Task<T> ExpectAsync<T>() where T : UBXModelBase
            {
                // Create expecting description as key
                var expectingDesc = new ExpectingDescription(typeof(T));

                // invalid operation if other context already expecting the same type at the same time
                if (expectingList.ContainsKey(expectingDesc))
                    throw new InvalidOperationException(String.Format("Already expecting type {0}", typeof(T).FullName));

                // Create expecting context
                var expectingContext = new ExpectingContext();

                // Add to expecting list
                expectingList.Add(expectingDesc, expectingContext);

                // Wait indefinitely until the response of the type is received
                await expectingContext.WaitForResponse();
                // At this point, the message is received
                // Remove from expecting list
                expectingList.Remove(expectingDesc);

                // Return the message object to the caller
                return (T)expectingContext.ResponseReceived;
            }

            /// <summary>
            /// Expect an Acknowledge message from GPS serial device
            /// </summary>
            /// <param name="classId">Class ID of expected acknowledge message</param>
            /// <param name="messageId">Message ID of expected acknowledge message</param>
            /// <returns>True if ACK message is received, False if NOT-ACK message is received</returns>
            /// <remarks>
            /// Use this method to wait acknowledge message (ACK/NOT-ACK) from serial device after transmitting
            /// configuration message to the device. This method will wait indefinitely until ACK/NOT-ACK message
            /// for specified class and message ID is received.
            /// </remarks>
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

                if (retVal is Acknowledge)
                    return true;
                else
                    return false;
            }

            /// <summary>
            /// Abort the asynchronous ExpectAsync operation.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <remarks>
            /// This method is used if you want to implement timeout on <see cref="ExpectAsync{T}"/> execution.
            /// The ExpectAsync method will return null if it is awaited.
            /// </remarks>
            public void AbortExpectAsync<T>()
            {
                var expectingDesc = new ExpectingDescription(typeof(T));
                if (!expectingList.ContainsKey(expectingDesc))
                    return;

                var expectingContext = expectingList[expectingDesc];
                expectingList.Remove(expectingDesc);
                expectingContext.Cancel();
            }

            /// <summary>
            /// Notify any party if waits a message of this type
            /// </summary>
            /// <param name="obj">The message to be notified</param>
            /// <returns>True if there is a party that is waiting for this kind of message, false otherwise.</returns>
            public bool NotifyIfExpected(UBXModelBase obj)
            {
                var expectingDesc = new ExpectingDescription(obj);
                try
                {
                    var expectingContext = expectingList[expectingDesc];
                    expectingContext.NotifyResponseReceived(obj);
                    return true;
                }
                catch (KeyNotFoundException)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Class that represent the key of expected message
        /// </summary>
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

            /// <summary>
            /// Create description key for expecting a message of type.
            /// </summary>
            /// <param name="message">The message</param>
            public ExpectingDescription(UBXModelBase message)
            {
                if (message is AcknowledgeBase)
                {
                    var ack = message as AcknowledgeBase;
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

            /// <summary>
            /// Create description key for expecting a message of type.
            /// </summary>
            /// <param name="message">The type of the message</param>
            public ExpectingDescription(Type messageType)
            {
                this.expectingType = messageType;
                this.expectedMessageMode = ExpectingMode.Regular;
            }

            /// <summary>
            /// Create description key for expecting an acknowledge message
            /// </summary>
            /// <param name="classId">Class ID that is expected</param>
            /// <param name="messageId">Message ID that is expected</param>
            public ExpectingDescription(byte classId, byte messageId)
            {
                this.classId = classId;
                this.messageId = messageId;
                this.expectedMessageMode = ExpectingMode.Acknowledge;
            }

            public override bool Equals(object obj)
            {
                if (base.Equals(obj))
                    return true;
                else
                {
                    ExpectingDescription desc = obj as ExpectingDescription;
                    if (desc != null)
                    {
                        if (this.expectedMessageMode == desc.expectedMessageMode)
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

        /// <summary>
        /// Context of expecting mechanism
        /// </summary>
        private class ExpectingContext
        {
            private CancellationTokenSource notifyTokenSource;

            /// <summary>
            /// Instantiate ExpectingContext
            /// </summary>
            public ExpectingContext()
            {
                notifyTokenSource = new CancellationTokenSource();
            }

            /// <summary>
            /// The response message received
            /// </summary>
            public UBXModelBase ResponseReceived { get; private set; }

            /// <summary>
            /// Wait indefinitely for response message
            /// </summary>
            public async Task WaitForResponse()
            {
                try
                {
                    await Task.Delay(-1, notifyTokenSource.Token);
                }
                catch (TaskCanceledException)
                {

                }
            }

            /// <summary>
            /// Notify the waiting party that the response is received
            /// </summary>
            /// <param name="obj">The message that is received</param>
            public void NotifyResponseReceived(UBXModelBase obj)
            {
                this.ResponseReceived = obj;
                notifyTokenSource.Cancel();
            }

            /// <summary>
            /// Cancel the wait operation
            /// </summary>
            public void Cancel()
            {
                notifyTokenSource.Cancel();
            }
        }


        /// <summary>
        /// Default buffer length
        /// </summary>
        public const uint BufferLength = 10240;

        private DeviceInformation deviceInfo;
        private CancellationTokenSource cancelToken;

        private Configuration.Port portConfig;

        private Queue<UBXModelBase> transmitQueue = new Queue<UBXModelBase>();
        private Dictionary<UBXModelBase, CancellationTokenSource> transmissionNotification = new Dictionary<UBXModelBase, CancellationTokenSource>();

        private ExpectingList expectingList = new ExpectingList();

        private uint baudRate;
        private Task runningListenTask;

        bool running = false;

        /// <summary>
        /// Instantiate UBX Serial GPS device
        /// </summary>
        /// <param name="deviceInfo">Serial device information</param>
        /// <param name="portConfig">Port configuration for UBX device</param>
        public UBXSerialGPS(DeviceInformation deviceInfo, Configuration.Port portConfig)
        {
            this.deviceInfo = deviceInfo;
            this.portConfig = portConfig;
            this.baudRate = AvailableBaudRate[1]; // Default BaudRate
        }

        /// <summary>
        /// Initialize and spawn serial device based on DeviceInformation and baud rate
        /// </summary>
        /// <param name="info">The device information for spawning the serial device</param>
        /// <param name="baudRate">Specified baud rate</param>
        /// <returns>The initialized SerialDevice</returns>
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

        /// <summary>
        /// Start the UBXSerialDevice operation
        /// </summary>
        /// <returns>Task that represent the asynchronous operation of starting the device</returns>
        /// <remarks>
        /// The method first perform autodetecting port speed. After the current port speed is probed,
        /// the method will try to config the port based on specified port configutation. The method 
        /// completes the operation after initializing the internal asynchronous task for communication 
        /// loop, which will runs asynchronously transparent from the user of this class.
        /// </remarks>
        public async Task Start()
        {
            // Autodetect current baud rate
            await AutodetectPortSpeed();
            //this.baudRate = 115200;

            // Config the port
            await TryConfigPort();

            this.runningListenTask = Listen(this.baudRate);
        }

        /// <summary>
        /// Perform autodetecting port speed that the device is currently configured
        /// </summary>
        /// <returns>Task that represent the asynchronous operation for autodetecting port speed</returns>
        private async Task AutodetectPortSpeed()
        {
            // Loop on each supported baud rate
            foreach (uint currentBaud in AvailableBaudRate)
            {
#if DEBUG
                Debug.WriteLine(String.Format("Probing speed: {0}", currentBaud));
#endif
                var success = await ProbePortSpeed(currentBaud);
                if (success)
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

        /// <summary>
        /// Perform device's port configuration
        /// </summary>
        /// <returns>Task that represent the asyncrhonous operation for configuring the device's port</returns>
        private async Task TryConfigPort()
        {
            if (portConfig == null)
                return;

            try
            {
                // Open and listen to serial device
                this.runningListenTask = Listen(this.baudRate);

                // Write the configuration data
                await this.WriteConfigIgnoreResultAsync(portConfig);

                // Assume that the baud rate is changed
                // UBX will not send ACK on this current baud rate if the baud rate
                // is changed. So it is no use to wait for ACK message to ensure that
                // the config message is acknowledged
                this.baudRate = portConfig.BaudRate;
#if DEBUG
                Debug.WriteLine("Success config BaudRate to: {0}", this.baudRate);
#endif

            }
            finally
            {
                // Stop the listening loop to serial device
                await Stop();
            }
        }

        /// <summary>
        /// Probe port speed that is currently running on the serial device
        /// </summary>
        /// <param name="testingBaudRate">Baud rate to probe</param>
        /// <returns>True if the <paramref name="testingBaudRate"/> is correct baud rate of the device, false otherwise</returns>
        private async Task<bool> ProbePortSpeed(uint testingBaudRate)
        {
            try
            {
                // Open and start listening to serial device
                this.runningListenTask = Listen(testingBaudRate);

                await Task.Delay(2000);

                // Poll message from device
                var task = this.PollMessageAsync<Navigation.Clock>();

                // Wait until timeout
                if (await Task.WhenAny(task, Task.Delay(10000)) == task)
                {
                    // Get the result
                    var result = await task;

                    // If the result is exist, it is correct baud rate
                    // otherwise, no
                    if (result == null)
                        return false;
                    else
                        return true;
                }
                else
                {
                    // This is timeout, the message never received properly
                    // So it is incorrect baud rate
                    this.AbortPollMessageAsync<Navigation.Clock>();
                    return false;
                }
            }
            finally
            {
                await Stop();
            }
        }

        /// <summary>
        /// Stop the communication loop of the serial device
        /// </summary>
        /// <returns>Task that represent the asyncrhonous operation for stopping the device's communication</returns>
        public async Task Stop()
        {
            if (this.runningListenTask == null)
                return;

            // Notify the cancellation
            cancelToken.Cancel();

            try
            {
                // Wait indefinitely until the listening loop has shutted down completely
                await runningListenTask;
            }
            catch (TaskCanceledException)
            {

            }
            finally
            {
                // Clear the task
                runningListenTask = null;
            }

        }

        /// <summary>
        /// Communication loop for GPS serial device
        /// </summary>
        /// <param name="baudRate">Baud rate for this communication loop</param>
        /// <returns>Task that represent the asyncrhonous operation of communication loop</returns>
        private async Task Listen(uint baudRate)
        {
            // If it is already running, just bye
            if (running)
                return;

            // Initialize state and cancellation token to stop this operation
            running = true;
            cancelToken = new CancellationTokenSource();

            DataReader dataReader = null;
            DataWriter dataWriter = null;
            SerialDevice serialPort = null;

#if DEBUG
            Debug.WriteLine("Listening start...");
#endif

            try
            {
                // Get serial device from device info
                serialPort = await InitializeSerialPort(this.deviceInfo, baudRate);
                
                serialPort.WriteTimeout = TimeSpan.FromMilliseconds(100);
                serialPort.ReadTimeout = TimeSpan.FromMilliseconds(100);

                dataReader = new DataReader(serialPort.InputStream);
                dataReader.InputStreamOptions = InputStreamOptions.Partial;

                dataWriter = new DataWriter(serialPort.OutputStream);

                // Queue to store current byte processed, in case of data is transmitted
                // intersecting between two loop
                Queue<byte> currentlyProcessed = new Queue<byte>(1024);

                // Current communication state of the loop
                byte currentState = 0;
                ushort payloadLength = 0;

                while (true)
                {
                    cancelToken.Token.ThrowIfCancellationRequested();

                    // Write any queued message
                    await WriteQueuedMessages(dataWriter);

                    // Longer timeout to prevent package drop
                    CancellationTokenSource timeoutToken = new CancellationTokenSource(serialPort.ReadTimeout.Milliseconds * 10);

                    // Load asynchronously, while at the same time we process the data that is ready
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
                                if (currentByte != UBXModelBase.Header1)
                                    fail = true;
                                break;
                            case 1: // Followed by Header 2, otherwise fail
                                if (currentByte != UBXModelBase.Header2)
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
                        else if (currentState != 6)
                        {
                            // Increment state
                            currentState++;
                        }
                        else if (currentState == 6)
                        {
                            // Loading the payload
                            if (payloadLength > 0)
                                payloadLength--;
                            else
                                currentState++;
                        }

                        // If the entire message has been received properly
                        // perform processing
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
                                // Parse the byte to UBX models
                                var package = UBXModelBase.TryParse(arr);

                                // Notify if any party is waiting this kind of message
                                if (!expectingList.NotifyIfExpected(package))
                                {
                                    OnMessageReceived(package);
                                }
#if DEBUG
                                Debug.WriteLine(package.ToString());
#endif
                            }
                            catch (UBXException ex)
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
                    catch (Exception ex)
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
            catch (Exception ex)
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

                if (serialPort != null)
                {
                    serialPort.Dispose();
                    serialPort = null;
                }

                running = false;
                cancelToken = null;
            }
        }

        /// <summary>
        /// Write queued message to a data writer specified
        /// </summary>
        /// <param name="writer">Data writer where the data will be written to</param>
        /// <returns>Task that represent the asyncrhonous operation of WriteQueuedMessages</returns>
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

                if (this.transmissionNotification.ContainsKey(currentTransmission))
                {
                    transmissionNotification[currentTransmission].Cancel();
                }
#if DEBUG
                Debug.WriteLine("Transmitting package completed: " + bytes + " bytes written");
#endif
            }
        }

        /// <summary>
        /// Transmit any message to the device
        /// </summary>
        /// <param name="messageToTransmit">Message to transmit to the device</param>
        public void TransmitMessage(UBXModelBase messageToTransmit)
        {
            transmitQueue.Enqueue(messageToTransmit);
        }

        /// <summary>
        /// Perform writing binary data to the DataWriter
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="b"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Write configuration message to the device and wait until acknowledge or not-acknowledge message
        /// arrived
        /// </summary>
        /// <param name="data">Communication message to be transmitted </param>
        /// <returns>
        /// Task represents the WriteConfigAsync execution, which upon completion returns boolean where 
        /// true means the configuration is acknowledged, and false otherwise.
        /// </returns>
        public async Task<bool> WriteConfigAsync(UBXModelBase data)
        {
            if (!UBXModelBase.IsConfigMessage(data))
                throw new NotSupportedException("WriteConfig only available for config type UBX message");

            TransmitMessage(data);
            var attr = UBXModelBase.GetMessageAttribute(data);
            return await expectingList.ExpectAcknowledgeAsync(attr.ClassID, attr.MessageID);
        }

        /// <summary>
        /// Write configuration message and wait for the transmission to complete without caring about
        /// it being acknowledge or not.
        /// </summary>
        /// <param name="data">Communication message to be transmitted</param>
        /// <returns>Task represent the WriteConfigIgnoreResultAsync execution</returns>
        public async Task WriteConfigIgnoreResultAsync(UBXModelBase data)
        {
            if (!UBXModelBase.IsConfigMessage(data))
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

        /// <summary>
        /// Asynchronously request for message of type from the device.
        /// </summary>
        /// <typeparam name="T">Type of message that is requested.</typeparam>
        /// <returns>
        /// Task represents the PollMessageAsync execution, which upon completion returns the data
        /// requested from the device, or null if it is aborted by calling <see cref="AbortPollMessageAsync{T}"/>.
        /// </returns>
        public async Task<T> PollMessageAsync<T>() where T : UBXModelBase
        {
            var pollMessage = UBXModelBase.GetPollMessage<T>();

            // Send the data
            TransmitMessage(pollMessage);

            // Wait until expected response comes
            return await expectingList.ExpectAsync<T>();
        }

        /// <summary>
        /// Abort the asynchronous call of <see cref="PollMessageAsync{T}"/>.
        /// </summary>
        /// <typeparam name="T">Type of message that was expected which is going to be aborted.</typeparam>
        /// <remarks>
        /// Call this method on different asynchronous context than the calling of <see cref="PollMessageAsync{T}"/>.
        /// Calling this function will result the awaiting party of <see cref="PollMessageAsync{T}"/> continues the
        /// exectuion and receive null as the return.
        /// </remarks>
        public void AbortPollMessageAsync<T>() where T : UBXModelBase
        {
            expectingList.AbortExpectAsync<T>();
        }

        /// <summary>
        /// When a data or message is received from the GPS module.
        /// </summary>
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        /// <summary>
        /// Raise MessageReceived event with the specified message object.
        /// </summary>
        /// <param name="receivedMessage">Message object that is just received.</param>
        protected void OnMessageReceived(UBXModelBase receivedMessage)
        {
            this.MessageReceived?.Invoke(this, new MessageReceivedEventArgs(receivedMessage));
        }
    }

    /// <summary>
    /// Event arguments for MessageReceived event.
    /// </summary>
    public class MessageReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// The received message.
        /// </summary>
        public UBXModelBase ReceivedMessage { get; private set; }

        /// <summary>
        /// Instantiate MessageReceivedEventArgs.
        /// </summary>
        /// <param name="receivedMessage">The received message object.</param>
        internal MessageReceivedEventArgs(UBXModelBase receivedMessage)
        {
            this.ReceivedMessage = receivedMessage;
        }
    }
}
