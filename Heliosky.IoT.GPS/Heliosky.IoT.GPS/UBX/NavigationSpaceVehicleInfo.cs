using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heliosky.IoT.GPS.UBX
{
    [UBXMessage(0x01, 0x30, MessageType.Receive | MessageType.Poll)]
    public class NavigationSpaceVehicleInfo
    {
        [UBXField(1)]
        public uint TimeMillisOfWeek { get; set; }

        [UBXField(2)]
        public byte ChannelCount { get; set; }

        [UBXField(3)]
        public byte GlobalFlags { get; private set; }

        [UBXField(4)]
        private ushort Reserved2 { get; set; }

        [UBXField(5)]
        [UBXList(2)]
        private IEnumerable<SpaceVehicleChannelItem> ChannelList { get; set; }
    }

    [UBXStructure]
    public struct SpaceVehicleChannelItem
    {
        [UBXField(1)]
        public byte ChannelNumber { get; set; }

        [UBXField(2)]
        public byte SatteliteID { get; set; }

        [UBXField(3)]
        public byte Flags { get; set; }

        [UBXField(4)]
        public byte Quality { get; set; }

        [UBXField(5)]
        public byte SignalStrength { get; set; }

        [UBXField(6)]
        public sbyte Elevation { get; set; }

        [UBXField(7)]
        public short Azimuth { get; set; }

        [UBXField(8)]
        public int PseudoRangeResidual { get; set; }
    }
}
