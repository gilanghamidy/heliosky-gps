using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;

namespace Heliosky.IoT.GPS
{
    public class SerialGPS
    {
        private DeviceInformation deviceInfo;

        public SerialGPS(DeviceInformation deviceInfo)
        {
            this.deviceInfo = deviceInfo;
        }


    }
}
