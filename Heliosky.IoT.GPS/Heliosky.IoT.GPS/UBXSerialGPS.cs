using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;

namespace Heliosky.IoT.GPS
{
    public class UBXSerialGPS
    {
        private DeviceInformation deviceInfo;
        private SerialDevice serialPort;
        private CancellationTokenSource cancelToken;

        bool running = false;

        public UBXSerialGPS(DeviceInformation deviceInfo)
        {
            this.deviceInfo = deviceInfo;
        }
    }
}
