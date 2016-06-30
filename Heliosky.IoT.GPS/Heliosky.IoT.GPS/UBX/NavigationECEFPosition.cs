using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heliosky.IoT.GPS.UBX
{
    [UBXMessage(0x01, 0x01, MessageType.Receive | MessageType.Poll)]
    public class NavigationECEFPosition : UBXModelBase
    {
        [UBXField(0)]
        public uint TimeMillisOfWeek { get; set; }

        [UBXField(1)]
        public int X { get; set; }

        [UBXField(2)]
        public int Y { get; set; }

        [UBXField(3)]
        public int Z { get; set; }

        [UBXField(4)]
        public uint Accuracy { get; set; }
    }
}
