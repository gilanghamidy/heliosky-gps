/*   Status.cs
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


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heliosky.IoT.GPS.Navigation
{
    [UBXMessage(0x01, 0x03, MessageType.Receive | MessageType.Poll)]
    public class Status : UBXModelBase
    {
        [UBXField(0)]
        public uint TimeMillisOfWeek { get; private set; }

        [UBXField(1)]
        private byte FixStatusTypeValue { get; set; }

        public FixStatus FixStatusType
        {
            get { return (Navigation.FixStatus)FixStatusTypeValue; }
        }

        [UBXField(2)]
        private byte StatusFlagRaw { get; set; }

        [UBXField(3)]
        private byte FixStatusFlag { get; set; }

        [UBXField(4)]
        private byte FixStatusFlag2 { get; set; }

        [UBXField(5)]
        public uint TimeToFirstFix { get; set; }

        [UBXField(6)]
        public uint TimeSinceStartUp { get; set; }
    }

    public enum FixStatus
    {
        NoFix = 0,
        DeadReckoning = 1,
        Fix2D = 2,
        Fix3D = 3,
        GPSDeadReckoning = 4,
        TimeOnly = 5
    }
}
