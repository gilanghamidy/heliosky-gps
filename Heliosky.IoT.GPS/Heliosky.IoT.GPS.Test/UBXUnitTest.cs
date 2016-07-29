/* The MIT License (MIT)
 * UBXUnitTest.cs
 * Copyright (c) 2016 Gilang M. Hamidy (gilang.hamidy@gmail.com)
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation
 * the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 * DEALINGS IN THE SOFTWARE.
 */

using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

namespace Heliosky.IoT.GPS.Test
{
    [TestClass]
    public class UBXUnitTest
    {
        [ClassInitialize]
        public static void InitializeTest(TestContext ctx)
        {
            // Force initialize static methods
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(UBXModelBase).TypeHandle);
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(BinaryWriterHelper).TypeHandle);
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(BinaryReaderHelper).TypeHandle);
        }

        [TestMethod]
        public void GenerateConfigPortMessage()
        {
            Configuration.Port cfg_prt = new Configuration.Port()
            {
                PortID = 1,
                StopBit = Configuration.Port.StopBitType.OneStop,
                Parity = Configuration.Port.ParityType.NoParity,
                CharacterLength = Configuration.Port.CharacterLengthType.Bit8,
                BaudRate = 115200,
                InputProtocol = Configuration.Port.Protocol.UBX | Configuration.Port.Protocol.NMEA,
                OutputProtocol = Configuration.Port.Protocol.UBX | Configuration.Port.Protocol.NMEA
            };

            var generated_data = cfg_prt.ToBinaryData();

            byte[] correct = { 0xB5, 0x62, 0x06, 0x00,
                               0x14, 0x00, 0x01, 0x00,
                               0x00, 0x00, 0xD0, 0x08,
                               0x00, 0x00, 0x00, 0xC2,
                               0x01, 0x00, 0x03, 0x00,
                               0x03, 0x00, 0x00, 0x00,
                               0x00, 0x00, 0xBC, 0x5E };

            Assert.AreEqual(correct.Length, generated_data.Length, "Content Length");
            
            for(int i = 0; i < correct.Length; i++)
            {
                if(correct[i] != generated_data[i])
                {
                    Assert.Fail("Incorrect data on index: " + i);
                    break;
                }
            }
        }

        [TestMethod]
        public void ParseConfigPortMessage()
        {
            byte[] message = { 0xB5, 0x62, 0x06, 0x00,
                               0x14, 0x00, 0x01, 0x00,
                               0x00, 0x00, 0xD0, 0x08,
                               0x00, 0x00, 0x00, 0xC2,
                               0x01, 0x00, 0x03, 0x00,
                               0x03, 0x00, 0x00, 0x00,
                               0x00, 0x00, 0xBC, 0x5E };

            var parsed = (Configuration.Port)Configuration.Port.TryParse(message);

            Assert.AreEqual(1, parsed.PortID);
            Assert.AreEqual(Configuration.Port.StopBitType.OneStop, parsed.StopBit);
            Assert.AreEqual(Configuration.Port.ParityType.NoParity, parsed.Parity);
            Assert.AreEqual(Configuration.Port.CharacterLengthType.Bit8, parsed.CharacterLength);
            Assert.AreEqual((uint)115200, parsed.BaudRate);
            Assert.AreEqual(Configuration.Port.Protocol.UBX | Configuration.Port.Protocol.NMEA, parsed.InputProtocol);
            Assert.AreEqual(Configuration.Port.Protocol.UBX | Configuration.Port.Protocol.NMEA, parsed.OutputProtocol);
        }

        [TestMethod]
        public void ParseAcknowledgeMessage()
        {
            byte[] message = { 0xB5, 0x62, 0x05, 0x01, 0x02,
                               0x00, 0x06, 0x00, 0x0E, 0x37 };

            var parsed = (Acknowledge)UBXModelBase.TryParse(message);

            Assert.AreEqual((byte)0x06, parsed.ClassID);
            Assert.AreEqual((byte)0x00, parsed.MessageID);
        }
    }
}
