using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heliosky.IoT.GPS
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
