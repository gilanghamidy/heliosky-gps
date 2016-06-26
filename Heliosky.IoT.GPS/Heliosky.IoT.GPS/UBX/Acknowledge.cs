using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heliosky.IoT.GPS.UBX
{
    
    public class AcknowledgeBase : UBXModelBase
    {
        [UBXField(1)]
        public byte ClassID { get; set; }

        [UBXField(2)]
        public byte MessageID { get; set; }
    }

    [UBXMessage(0x05, 0x01, MessageType.Receive)]
    public class Acknowledge : AcknowledgeBase
    {

    }

    [UBXMessage(0x05, 0x00, MessageType.Receive)]
    public class NotAcknowledge : AcknowledgeBase
    {

    }
}
