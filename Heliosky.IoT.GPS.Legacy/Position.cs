/*   Position.cs
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

namespace Heliosky.IoT.GPS.Legacy
{
    public struct Position
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public static Position FromDegree(LatitudeDegree lat, LongitudeDegree lng)
        {
            Position p = new Position();
            p.Latitude = lat.Degree + lat.Minutes * 60 * (lat.Direction == LatitudeDegree.DirectionType.North ? 1 : -1);
            p.Longitude = lng.Degree + lng.Minutes * 60 * (lng.Direction == LongitudeDegree.DirectionType.East ? 1 : -1);

            return p;
        }
    }
}
