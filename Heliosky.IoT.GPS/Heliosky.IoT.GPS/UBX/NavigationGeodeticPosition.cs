using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heliosky.IoT.GPS.UBX
{
    [UBXMessage(0x01, 0x02, MessageType.Receive | MessageType.Poll)]
    public class NavigationGeodeticPosition : UBXModelBase
    {
        [UBXField(0)]
        public uint TimeMillisOfWeek { get; set; }

        [UBXField(1)]
        public int LongitudeValue { get; private set; }

        [UBXField(2)]
        public int LatitudeValue { get; private set; }

        [UBXField(3)]
        public int HeightAboveEllipsoid { get; set; }

        [UBXField(4)]
        public int HeightAboveSeaLevel { get; set; }

        [UBXField(5)]
        public uint HorizontalAccuracy { get; set; }

        [UBXField(6)]
        public uint VerticalAccuracy { get; set; }

        public double Latitude
        {
            get { return LatitudeValue / 10000000.0; }
        }

        public double Longitude
        {
            get { return LongitudeValue / 10000000.0; }
        }

        public override string ToString()
        {
            StringBuilder bldr = new StringBuilder();

            bldr.AppendLine("Navigation Geodesic Position");
            bldr.AppendLine("Latitude: " + Latitude);
            bldr.AppendLine("Longitude: " + Longitude);
            bldr.AppendLine("Height Above Sea Level: " + (HeightAboveSeaLevel / 1000.0) + " m");
            bldr.AppendLine("Height Above Ellipsoid: " + (HeightAboveEllipsoid / 1000.0) + " m");
            bldr.AppendLine("Horizontal Accuracy: " + (HorizontalAccuracy / 1000.0) + " m");
            bldr.AppendLine("Vertical Accuracy: " + (VerticalAccuracy / 1000.0) + " m");
            bldr.AppendLine("Time of Week: " + TimeMillisOfWeek + " ms");


            return bldr.ToString();
        }

    }
}
