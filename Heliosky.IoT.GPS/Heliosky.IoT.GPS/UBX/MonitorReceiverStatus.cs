using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heliosky.IoT.GPS.UBX
{
    [UBXMessage(0x0A, 0x21, MessageType.Receive | MessageType.Poll)]
    public class MonitorReceiverStatus : UBXModelBase
    {
        [UBXField(0)]
        private byte Flag { get; set; }

        public bool Awake
        {
            get { return Flag != 0; }
        }
    }
}
