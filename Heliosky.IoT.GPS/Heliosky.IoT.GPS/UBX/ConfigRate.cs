using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heliosky.IoT.GPS.UBX
{
    [UBXMessage(0x06, 0x08, MessageType.Send | MessageType.Receive)]
    public class ConfigRate : UBXModelBase
    {
        public enum ReferenceTimeMode : ushort
        {
            UTCTime = 0,
            GPSTime = 1
        }

        [UBXField(1)]
        public ushort MeasurementRate { get; set; }

        [UBXField(2)]
        public ushort NavigationRate { get; set; }

        [UBXField(3)]
        public ushort ReferenceTimeValue { get; private set; }

        public ReferenceTimeMode ReferenceTime
        {
            get { return (ReferenceTimeMode)ReferenceTimeValue; }
            set { ReferenceTimeValue = (ushort)value; }
        }
    }
}
