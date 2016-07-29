using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
