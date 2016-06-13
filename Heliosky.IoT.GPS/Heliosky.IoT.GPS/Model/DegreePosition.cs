using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heliosky.IoT.GPS.Model
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
    }
}
