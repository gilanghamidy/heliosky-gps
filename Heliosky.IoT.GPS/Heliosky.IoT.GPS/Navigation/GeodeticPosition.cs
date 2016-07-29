/*   GeodeticPosition.cs
 *   Copyright (C) 2016 Gilang M. Hamidy (gilang.hamidy@gmail.com)
 *   
 *   This file is part of Heliosky.IoT.GPS
 * 
 *   Heliosky.IoT.GPS is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU Lesser General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   Heliosky.IoT.GPS is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU Lesser General Public License for more details.
 *
 *   You should have received a copy of the GNU Lesser General Public License
 *   along with Heliosky.IoT.GPS.  If not, see <http://www.gnu.org/licenses/>.
 */

using System.Text;
using Windows.Devices.Geolocation;

namespace Heliosky.IoT.GPS.Navigation
{
    [UBXMessage(0x01, 0x02, MessageType.Receive | MessageType.Poll)]
    public class GeodeticPosition : UBXModelBase
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

        public BasicGeoposition GetGeoposition()
        {
            var ret = new BasicGeoposition()
            {
                Latitude = this.Latitude,
                Longitude = this.Longitude,
                Altitude = this.HeightAboveSeaLevel / 1000.0
            };
            return ret;
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
