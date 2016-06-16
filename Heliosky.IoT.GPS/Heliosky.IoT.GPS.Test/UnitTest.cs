using System;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Heliosky.IoT.GPS;

namespace Heliosky.IoT.GPS.Test
{
    [TestClass]
    public class ParserLoadingTest
    {
        [TestMethod]
        public void LoadParserComponent()
        {
            var parser = new NMEAParser();
        }

        [TestMethod]
        public void ParseGPGGAString()
        {
            string str = "$GPGGA,123519,4807.038,N,01131.000,E,1,08,0.9,545.4,M,46.9,M,,*47";
            var parser = new NMEAParser();
            var res = parser.Parse(str);

            FixData data = (FixData)res;

            Assert.AreEqual(new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 12, 35, 19, 0), data.CurrentTime);
            Assert.AreEqual(48, data.Latitude.Value.Degree);
            Assert.AreEqual(7.038, data.Latitude.Value.Minutes);
            Assert.AreEqual(LatitudeDegree.DirectionType.North, data.Latitude.Value.Direction);

            Assert.AreEqual(11, data.Longitude.Value.Degree);
            Assert.AreEqual(31, data.Longitude.Value.Minutes);
            Assert.AreEqual(LongitudeDegree.DirectionType.East, data.Longitude.Value.Direction);

            Assert.AreEqual(8, data.SateliteUsed);
            Assert.AreEqual(0.9, data.HDOP);

            Assert.AreEqual(545.4, data.MeanSeaLevel);
            Assert.AreEqual(46.9, data.GeoidSeparation);
        }

        [TestMethod]
        public void ParseGPGGAStringWithNullField()
        {
            string str = "$GPGGA,,,,,,,,,,,99.99,M,,*35";

            var parser = new NMEAParser();
            var res = parser.Parse(str);

            FixData data = (FixData)res;
        }

        [TestMethod]
        public void ParseGPGGAStringFalseChecksum()
        {
            string str = "$GPGGA,123519,4807.038,N,01131.000,E,1,08,0.9,545.4,M,46.9,M,,*12";
            var parser = new NMEAParser();

            try
            {
                var res = parser.Parse(str);
                Assert.Fail("Parsing message with invalid checksum should fail");
            }
            catch(InvalidChecksumException)
            {
            
            }
        }

        [TestMethod]
        public void ParseUnknownNMEAString()
        {
            string str = "$GPABC,123519,4807.038,N,01131.000,E,1,08,0.9,545.4,M,46.9,M,,*46";
            var parser = new NMEAParser();

            try
            {
                var res = parser.Parse(str);
                Assert.Fail("Parsing message with unknown message type should fail");
            }
            catch(UnknownMessageException)
            {

            }
        }

        [TestMethod]
        public void ParseGPVTGString()
        {
            string str = "$GPVTG,055.7,T,034.4,M,005.5,N,010.2,K*49";
            var parser = new NMEAParser();
            var res = parser.Parse(str);

            CourseData data = (CourseData)res;

            Assert.AreEqual(55.7, data.COG);
            Assert.AreEqual(5.5, data.KnotSOG);
            Assert.AreEqual(10.2, data.KphSOG);
        }


    }
}
