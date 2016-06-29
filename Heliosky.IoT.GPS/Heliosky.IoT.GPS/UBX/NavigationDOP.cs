using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heliosky.IoT.GPS.UBX
{
    [UBXMessage(0x01, 0x04, MessageType.Receive | MessageType.Poll)]
    public class NavigationDOP : UBXModelBase
    {
        [UBXField(1)]
        public uint TimeMillisOfWeek { get; set; }

        [UBXField(2)]
        public ushort Geometric { get; set; }

        [UBXField(3)]
        public ushort Position { get; set; }

        [UBXField(4)]
        public ushort Time { get; set; }

        [UBXField(5)]
        public ushort Vertical { get; set; }

        [UBXField(6)]
        public ushort Horizontal { get; set; }

        [UBXField(7)]
        public ushort Northing { get; set; }

        [UBXField(8)]
        public ushort Easting { get; set; }
    }
}
