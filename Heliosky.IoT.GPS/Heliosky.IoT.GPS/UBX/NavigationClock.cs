using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heliosky.IoT.GPS.UBX
{
    [UBXMessage(0x01, 0x22, MessageType.Receive | MessageType.Poll)]
    public class NavigationClock : UBXModelBase
    {
        [UBXField(1)]
        public uint TimeMillisOfWeek { get; set; }

        [UBXField(2)]
        public int ClockBias { get; set; }

        [UBXField(3)]
        public int ClockDrift { get; set; }

        [UBXField(4)]
        public uint TimeAccurracy { get; set; }

        [UBXField(5)]
        public uint FrequencyAccuracy { get; set; }
    }
}
