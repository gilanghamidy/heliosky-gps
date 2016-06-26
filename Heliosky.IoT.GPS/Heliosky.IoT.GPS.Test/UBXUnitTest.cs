using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Heliosky.IoT.GPS;
using Heliosky.IoT.GPS;

namespace Heliosky.IoT.GPS.Test
{
    [TestClass]
    public class UBXUnitTest
    {
        [ClassInitialize]
        public static void InitializeTest(TestContext ctx)
        {
            // Force initialize static methods
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(UBX.UBXModelBase).TypeHandle);
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(UBX.BinaryWriterHelper).TypeHandle);
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(UBX.BinaryReaderHelper).TypeHandle);
        }

        [TestMethod]
        public void GenerateConfigPortMessage()
        {
            UBX.ConfigPort cfg_prt = new UBX.ConfigPort()
            {
                PortID = 1,
                StopBit = UBX.ConfigPort.StopBitType.OneStop,
                Parity = UBX.ConfigPort.ParityType.NoParity,
                CharacterLength = UBX.ConfigPort.CharacterLengthType.Bit8,
                BaudRate = 115200,
                InputProtocol = UBX.ConfigPort.Protocol.UBX | UBX.ConfigPort.Protocol.NMEA,
                OutputProtocol = UBX.ConfigPort.Protocol.UBX | UBX.ConfigPort.Protocol.NMEA
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

            var parsed = (UBX.ConfigPort)UBX.UBXModelBase.TryParse(message);

            Assert.AreEqual(1, parsed.PortID);
            Assert.AreEqual(UBX.ConfigPort.StopBitType.OneStop, parsed.StopBit);
            Assert.AreEqual(UBX.ConfigPort.ParityType.NoParity, parsed.Parity);
            Assert.AreEqual(UBX.ConfigPort.CharacterLengthType.Bit8, parsed.CharacterLength);
            Assert.AreEqual((uint)115200, parsed.BaudRate);
            Assert.AreEqual(UBX.ConfigPort.Protocol.UBX | UBX.ConfigPort.Protocol.NMEA, parsed.InputProtocol);
            Assert.AreEqual(UBX.ConfigPort.Protocol.UBX | UBX.ConfigPort.Protocol.NMEA, parsed.OutputProtocol);
        }

        [TestMethod]
        public void ParseAcknowledgeMessage()
        {
            byte[] message = { 0xB5, 0x62, 0x05, 0x01, 0x02,
                               0x00, 0x06, 0x00, 0x0E, 0x37 };

            var parsed = (UBX.Acknowledge)UBX.UBXModelBase.TryParse(message);

            Assert.AreEqual((byte)0x06, parsed.ClassID);
            Assert.AreEqual((byte)0x00, parsed.MessageID);
        }
    }
}
