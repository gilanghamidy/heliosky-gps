using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heliosky.IoT.GPS.Model
{
    public class GPSModel
    {

    }

    public class FixData : GPSModel
    {
        public DateTime CurrentTime { get; internal set; }
        public LatitudeDegree Latitude { get; internal set; }
        public LongitudeDegree Longitude { get; internal set; }
        public bool Valid { get; internal set; }
    }
}
