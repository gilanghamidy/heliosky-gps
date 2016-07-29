using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heliosky.IoT.GPS.Legacy
{
    public struct LatitudeDegree
    {
        public enum DirectionType
        {
            North, South
        }

        public int Degree { get; set; }
        public double Minutes { get; set; }
        public DirectionType Direction { get; set; }

        public override string ToString()
        {
            return Degree.ToString() + "\u00B0 " + Minutes.ToString() + "\" " + (Direction == DirectionType.North ? "N" : "S");
        }
    }

    public struct LongitudeDegree
    {
        public enum DirectionType
        {
            East, West
        }

        public int Degree { get; set; }
        public double Minutes { get; set; }
        public DirectionType Direction { get; set; }

        public override string ToString()
        {
            return Degree.ToString() + "\u00B0 " + Minutes.ToString() + "\" " + (Direction == DirectionType.East ? "E" : "W");
        }
    }
}
