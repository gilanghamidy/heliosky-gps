/*   NMEAParser.cs
 *   Copyright (C) 2016 Gilang M. Hamidy (gilang.hamidy@gmail.com)
 *   
 *   This file is part of Heliosky.IoT.GPS.Legacy
 * 
 *   Heliosky.IoT.GPS.Legacy is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU Lesser General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   Heliosky.IoT.GPS.Legacy is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU Lesser General Public License for more details.
 *
 *   You should have received a copy of the GNU Lesser General Public License
 *   along with Heliosky.IoT.GPS.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
