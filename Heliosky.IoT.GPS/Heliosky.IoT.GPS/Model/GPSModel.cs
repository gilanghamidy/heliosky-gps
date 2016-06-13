using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Heliosky.IoT.GPS.Model
{
    public class GPSModel
    {

    }

    

    [NMEAString("GPGGA")]
    public class FixData : GPSModel
    {
        [NMEAField(1)]
        public DateTime CurrentTime { get; internal set; }
        
        [NMEAField(2, DependentIndex = 3)]
        public LatitudeDegree Latitude { get; internal set; }

        [NMEAField(4, DependentIndex = 5)]
        public LongitudeDegree Longitude { get; internal set; }

        [NMEAField(9)]
        public double MeanSeaLevel { get; internal set; }

        [NMEAField(7)]
        public int SateliteUsed { get; internal set; }

        [NMEAField(8)]
        public double HDOP { get; internal set; }

        [NMEAField(11)]
        public double AltRef { get; internal set; }
    }
}
