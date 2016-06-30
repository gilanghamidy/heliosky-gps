using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heliosky.IoT.GPS.UBX
{
    public class UBXMessageReceivedEventArgs : EventArgs
    {
        public UBXModelBase Message { get; internal set; }
    }

    public delegate void UBXExpectedMessageCallback(UBXModelBase retrievedMessage);
}
