using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heliosky.IoT.GPS.Model
{
    public struct Position
    {
        public float Latitude { get; set; }
        public float Longitude { get; set; }

        public static Position FromDegree(LatitudeDegree lat, LongitudeDegree lng)
        {

        }
    }
}
