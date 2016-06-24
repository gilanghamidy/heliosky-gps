using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heliosky.IoT.GPS.UBX
{
    [UBXMessage(0x06, 0x00, MessageType.Send | MessageType.Receive)]
    public class ConfigPort : UBXModelBase
    {
        [Flags]
        public enum Protocol : ushort
        {
            UBX = 0x1,
            NMEA = 0x2
        }

        public enum CharacterLengthType : byte
        {
            Bit7 = 3,
            Bit8 = 4
        }

        public enum ParityType : byte
        {
            Even = 0,
            Odd = 1,
            NoParity = 4
        }

        public enum StopBitType : byte
        {
            OneStop = 0,
            OneHalf = 1,
            TwoStop = 2,
            TwoHalf = 3
        }

        public ConfigPort()
        {
            Mode = 0x10;
        }

        [UBXField(1)]
        public byte PortID { get; set; }

        [UBXField(2)]
        public byte Reserved1 { get; private set; }

        [UBXField(3)]
        public ushort TXReady { get; set; }

        [UBXField(4)]
        public uint Mode { get; private set; }

        [UBXField(5)]
        public uint BaudRate { get; set; }

        [UBXField(6)]
        public ushort InputProtocolMask { get; private set; }

        [UBXField(7)]
        public ushort OutputProtocolMask { get; private set; }

        [UBXField(8)]
        public ushort Reserved4 { get; private set; }

        [UBXField(9)]
        public ushort Reserved5 { get; private set; }

        public Protocol InputProtocol
        {
            get { return (Protocol)InputProtocolMask; }
            set { InputProtocolMask = (ushort)value; }
        }

        public Protocol OutputProtocol
        {
            get { return (Protocol)OutputProtocolMask; }
            set { OutputProtocolMask = (ushort)value; }
        }

        public CharacterLengthType CharacterLength
        {
            get
            {
                byte mask = 0xC0;
                byte typeVal = (byte)((Mode & mask) >> 6);
                return (CharacterLengthType)typeVal;
            }

            set
            {
                uint mask = ~0xC0U;
                uint clearedVal = Mode & mask;
                uint typeVal = (uint)value << 6;
                Mode = clearedVal | typeVal;
            }
        }

        public ParityType Parity
        {
            get
            {
                ushort mask = 0xE00;
                ushort typeVal = (ushort)((Mode & mask) >> 9);
                return (ParityType)typeVal;
            }

            set
            {
                uint mask = ~0xE00U;
                uint clearedVal = Mode & mask;
                uint typeVal = (uint)value << 9;
                Mode = clearedVal | typeVal;
            }
        }

        public StopBitType StopBit
        {
            get
            {
                ushort mask = 0x3000;
                ushort typeVal = (ushort)((Mode & mask) >> 12);
                return (StopBitType)typeVal;
            }

            set
            {
                uint mask = ~0x3000U;
                uint clearedVal = Mode & mask;
                uint typeVal = (uint)value << 12;
                Mode = clearedVal | typeVal;
            }
        }
    }
}
