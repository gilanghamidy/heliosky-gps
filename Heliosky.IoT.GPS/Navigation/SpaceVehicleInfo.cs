/*   SpaceVehicleInfo.cs
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

using System.Collections.Generic;
using System.Linq;

namespace Heliosky.IoT.GPS.Navigation
{
    [UBXMessage(0x01, 0x30, MessageType.Receive | MessageType.Poll)]
    public class SpaceVehicleInfo : UBXModelBase
    {
        [UBXField(0)]
        public uint TimeMillisOfWeek { get; set; }

        [UBXField(1)]
        public byte ChannelCount { get; set; }

        [UBXField(2)]
        public byte GlobalFlags { get; private set; }

        [UBXField(3)]
        private ushort Reserved2 { get; set; }

        [UBXField(4)]
        [UBXList(1)]
        public IEnumerable<SpaceVehicleChannelItem> ChannelList { get; set; }

        public override string ToString()
        {
            return "Navigation Space Vehicle Info. Count: " + ChannelList.Count();
        }
    }

    [UBXStructure]
    public struct SpaceVehicleChannelItem
    {
        [UBXField(0)]
        public byte ChannelNumber { get; set; }

        [UBXField(1)]
        public byte SatteliteID { get; set; }

        [UBXField(2)]
        public byte Flags { get; set; }

        [UBXField(3)]
        public byte Quality { get; set; }

        [UBXField(4)]
        public byte SignalStrength { get; set; }

        [UBXField(5)]
        public sbyte Elevation { get; set; }

        [UBXField(6)]
        public short Azimuth { get; set; }

        [UBXField(7)]
        public int PseudoRangeResidual { get; set; }
    }
}
