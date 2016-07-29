/*   Models.cs
 *   Copyright (C) 2016 Gilang M. Hamidy (gilang.hamidy@gmail.com)
 *   
 *   This file is part of Heliosky.IoT.GPS.Legacy
 * 
 *   Heliosky.IoT.GPS.Legacy is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU Lesser General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   Heliosky.IoT.GPS.Legacy is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU Lesser General Public License for more details.
 *
 *   You should have received a copy of the GNU Lesser General Public License
 *   along with Heliosky.IoT.GPS.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Heliosky.IoT.GPS.Legacy
{
    public class GPSModel
    {

    }

    [NMEAString("GPGGA")]
    public class FixData : GPSModel
    {
        [NMEAField(1)]
        public DateTime? CurrentTime { get; internal set; }

        [NMEAField(2, 3)]
        public LatitudeDegree? Latitude { get; internal set; }

        [NMEAField(4, 5)]
        public LongitudeDegree? Longitude { get; internal set; }

        [NMEAField(9)]
        public double? MeanSeaLevel { get; internal set; }

        [NMEAField(7)]
        public int? SateliteUsed { get; internal set; }

        [NMEAField(8)]
        public double? HDOP { get; internal set; }

        [NMEAField(11)]
        public double? GeoidSeparation { get; internal set; }
    }

    [NMEAString("GPVTG")]
    public class CourseData : GPSModel
    {
        [NMEAField(1)]
        public double COG { get; internal set; }

        [NMEAField(5)]
        public double KnotSOG { get; internal set; }

        [NMEAField(7)]
        public double KphSOG { get; internal set; }
    }
}
