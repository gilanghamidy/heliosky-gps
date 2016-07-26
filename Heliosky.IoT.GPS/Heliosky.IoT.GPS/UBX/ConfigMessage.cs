using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heliosky.IoT.GPS.UBX
{
    [UBXConfig]
    [UBXMessage(0x06, 0x01, MessageType.Send | MessageType.Receive)]
    public class ConfigMessage : UBXModelBase
    {
        [UBXField(0)]
        public byte ClassID { get; set; }

        [UBXField(1)]
        public byte MessageID { get; set; }

        [UBXField(2)]
        public byte Rate { get; set; }
    }
}
