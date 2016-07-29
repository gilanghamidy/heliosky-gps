using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Heliosky.IoT.GPS.Legacy
{
    public class NMEAParser
    {
        private const string NMEAStringRegex = @"^\$(([A-Z]+),(.*))\*([ABCDEF0-9]{2})$";

        private Dictionary<string, NMEAObjectParser> parserList;
        private Regex parsingRegex;

        public NMEAParser()
        {
            Assembly thisAssembly = typeof(NMEAParser).GetTypeInfo().Assembly;

            var items = from t in thisAssembly.GetTypes()
                        let attr = t.GetTypeInfo().GetCustomAttribute(typeof(NMEAStringAttribute)) as NMEAStringAttribute
                        where attr != null
                        select new { Type = t, Attribute = attr };

            this.parserList = items.ToDictionary(k => k.Attribute.Keyword, v => new NMEAObjectParser(v.Type));
            this.parsingRegex = new Regex(NMEAParser.NMEAStringRegex);
        }

        public GPSModel Parse(string input)
        {
            var parseMatch = parsingRegex.Match(input);

            GPSModel parsedModel = null;

            if(parseMatch.Success)
            {
                string keyword = parseMatch.Groups[2].Value;

                try
                {                    
                    byte textChecksum = Checksum(parseMatch.Groups[1].Value);
                    byte validChecksum = byte.Parse(parseMatch.Groups[4].Value, System.Globalization.NumberStyles.HexNumber);

                    if(textChecksum != validChecksum)
                    {
                        throw new InvalidChecksumException(String.Format("Checksum expected {0} while computed {1}", validChecksum, textChecksum));
                    }

                    
                    string[] objectContent = parseMatch.Groups[3].Value.Split(',');

                    parsedModel = this.parserList[keyword].Parse(objectContent);
                }
                catch (KeyNotFoundException)
                {
                    throw new UnknownMessageException(String.Format("Unknown message with type {0}", keyword));
                }
                catch (Exception)
                {
                    // Invalid string
                    parsedModel = null;
                    throw;
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

    

}
