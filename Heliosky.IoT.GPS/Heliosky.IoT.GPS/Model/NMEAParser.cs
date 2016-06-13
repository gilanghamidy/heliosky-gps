using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Heliosky.IoT.GPS.Model
{
    [System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    sealed class NMEAStringAttribute : Attribute
    {
        public NMEAStringAttribute(string nmeaKeyword, NMEAObjectParser parser)
        {
            this.Keyword = nmeaKeyword;
            this.Parser = parser;
        }

        public string Keyword { get; private set; }
        public NMEAObjectParser Parser { get; private set; }
    }

    public class NMEAParser
    {
        private const string NMEAStringRegex = @"^\$(([A-Z]+),(.*))\*([ABCDEF0-9]{2})$";

        private Dictionary<string, NMEAObjectParser> parserList;
        private Regex parsingRegex;

        public NMEAParser()
        {
            Assembly thisAssembly = typeof(NMEAParser).GetTypeInfo().Assembly;

            var items = from t in thisAssembly.GetTypes()
                        let attr = t.GetTypeInfo().GetCustomAttribute(typeof(NMEAStringAttribute))
                        where attr != null
                        select attr as NMEAStringAttribute;

            this.parserList = items.ToDictionary(k => k.Keyword, v => v.Parser);
            this.parsingRegex = new Regex(NMEAParser.NMEAStringRegex);
        }

        public GPSModel Parse(string input)
        {
            var parseMatch = parsingRegex.Match(input);

            GPSModel parsedModel = null;

            if(parseMatch.Success)
            {
                try
                {                    
                    byte textChecksum = Checksum(parseMatch.Captures[1].Value);
                    byte validChecksum = byte.Parse(parseMatch.Captures[4].Value, System.Globalization.NumberStyles.HexNumber);

                    if(textChecksum != validChecksum)
                    {
                        throw new Exception();
                    }

                    string keyword = parseMatch.Captures[2].Value;
                    string objectContent = parseMatch.Captures[3].Value;

                }
                catch (Exception)
                {
                    // Invalid string
                    parsedModel = null;
                }
            }
            
            return parsedModel;
        }

        private byte Checksum(string input)
        {
            byte checksum = (byte)input[0];
            for(int i = 1; i < input.Length; i++)
            {
                checksum = (byte)(checksum ^ (byte)input[i]);
            }

            return checksum;
        }
    }

    interface NMEAObjectParser
    {
        GPSModel Parse(string[] values);
    }
}
