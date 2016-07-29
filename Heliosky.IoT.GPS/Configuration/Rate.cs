/*   Rate.cs
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

namespace Heliosky.IoT.GPS.Configuration
{
    [UBXConfig]
    [UBXMessage(0x06, 0x08, MessageType.Send | MessageType.Receive)]
    public class Rate : UBXModelBase
    {
        public enum ReferenceTimeMode : ushort
        {
            UTCTime = 0,
            GPSTime = 1
        }

        [UBXField(0)]
        public ushort MeasurementRate { get; set; }

        [UBXField(1)]
        public ushort NavigationRate { get; set; }

        [UBXField(2)]
        public ushort ReferenceTimeValue { get; private set; }

        public ReferenceTimeMode ReferenceTime
        {
            get { return (ReferenceTimeMode)ReferenceTimeValue; }
            set { ReferenceTimeValue = (ushort)value; }
        }
    }
}
