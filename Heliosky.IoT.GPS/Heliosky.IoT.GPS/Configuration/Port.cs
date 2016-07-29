/*   Port.cs
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

namespace Heliosky.IoT.GPS.Configuration
{
    [UBXConfig]
    [UBXMessage(0x06, 0x00, MessageType.Send | MessageType.Receive)]
    public class Port : UBXModelBase
    {
        [Flags]
        public enum Protocol : ushort
        {
            UBX = 0x1,
            NMEA = 0x2
        }

        public enum CharacterLengthType : byte
        {
            Bit7 = 2,
            Bit8 = 3
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

        public Port()
        {
            Mode = 0x10;
        }

        [UBXField(0)]
        public byte PortID { get; set; }

        [UBXField(1)]
        public byte Reserved1 { get; private set; }

        [UBXField(2)]
        public ushort TXReady { get; set; }

        [UBXField(3)]
        public uint Mode { get; private set; }

        [UBXField(4)]
        public uint BaudRate { get; set; }

        [UBXField(5)]
        private ushort InputProtocolMask { get; set; }

        [UBXField(6)]
        public ushort OutputProtocolMask { get; private set; }

        [UBXField(7)]
        public ushort Reserved4 { get; private set; }

        [UBXField(8)]
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
