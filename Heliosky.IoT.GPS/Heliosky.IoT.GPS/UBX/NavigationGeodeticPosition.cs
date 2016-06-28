using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heliosky.IoT.GPS.UBX
{
    [UBXMessage(0x01, 0x02, MessageType.Receive)]
    public class NavigationGeodeticPosition : UBXModelBase
    {
        [UBXField(1)]
        public uint TimeMillisOfWeek { get; set; }

        [UBXField(2)]
        public int LongitudeValue { get; private set; }

        [UBXField(3)]
        public int LatitudeValue { get; private set; }

        [UBXField(4)]
        public int HeightAboveEllipsoid { get; set; }

        [UBXField(5)]
        public int HeightAboveSeaLevel { get; set; }

        [UBXField(6)]
        public uint HorizontalAccuracy { get; set; }

        [UBXField(7)]
        public uint VerticalAccuracy { get; set; }

    }
}
