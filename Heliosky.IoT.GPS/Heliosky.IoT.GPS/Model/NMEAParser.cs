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
        public NMEAStringAttribute(string nmeaKeyword)
        {
            this.Keyword = nmeaKeyword;
        }

        public string Keyword { get; private set; }
    }

    [System.AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    sealed class NMEAFieldAttribute : Attribute
    {
        // This is a positional argument
        public NMEAFieldAttribute(int index)
        {
            this.Index = index;
        }
        
        public int Index { get; private set; }
        public int DependentIndex { get; set; }
    }

    [System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    sealed class NMEAFieldParserAttribute : Attribute
    {
        // This is a positional argument
        public NMEAFieldParserAttribute()
        {

        }
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
                try
                {                    
                    byte textChecksum = Checksum(parseMatch.Groups[1].Value);
                    byte validChecksum = byte.Parse(parseMatch.Groups[4].Value, System.Globalization.NumberStyles.HexNumber);

                    if(textChecksum != validChecksum)
                    {
                        throw new Exception();
                    }

                    string keyword = parseMatch.Groups[2].Value;
                    string[] objectContent = parseMatch.Groups[3].Value.Split(',');

                    parsedModel = this.parserList[keyword].Parse(objectContent);
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

    public static class NMEAFormat
    {
        private static Regex timeFormatRegex;
        private static Regex degreeFormatRegex;
        private static Dictionary<Type, MethodInfo> fieldParserMethod;

        static NMEAFormat()
        {
            timeFormatRegex = new Regex(@"([0-2][0-9])([0-5][0-9])([0-5][0-9])\.([0-9]{2})");
            degreeFormatRegex = new Regex(@"([0-9]{2,3})([0-9]{2}\.[0-9]{4})");

            var internalParser = from method in typeof(NMEAFormat).GetTypeInfo().DeclaredMethods
                                 let attr = method.GetCustomAttribute(typeof(NMEAFieldAttribute)) as NMEAFieldParserAttribute
                                 where attr != null
                                 select new { method.ReturnType, method };

            // Internal parser
            fieldParserMethod = internalParser.ToDictionary(k => k.ReturnType, v => v.method);

            // Double parser
            fieldParserMethod.Add(typeof(double), typeof(double).GetMethod("Parse", new Type[] { typeof(string) }));

            // Integer parser
            fieldParserMethod.Add(typeof(int), typeof(int).GetMethod("Parse", new Type[] { typeof(string) }));

            
        }


        public static object ParseValue(Type targetType, string value, string dependent)
        {
            return null;
        }

        [NMEAFieldParser]
        public static DateTime ParseTime(string time)
        {
            var timeMatcher = timeFormatRegex.Match(time);

            if (!timeMatcher.Success)
            {
                return DateTime.MinValue;
            }

            DateTime ret = new DateTime(
                year: DateTime.Now.Year,
                month: DateTime.Now.Month,
                day: DateTime.Now.Day,
                hour: int.Parse(timeMatcher.Captures[1].Value),
                minute: int.Parse(timeMatcher.Captures[2].Value),
                second: int.Parse(timeMatcher.Captures[3].Value),
                millisecond: int.Parse(timeMatcher.Captures[4].Value) * 10);

            return ret;

        }

        [NMEAFieldParser]
        public static LatitudeDegree ParseLatitude(string degree, string direction)
        {
            if (direction != "N" || direction != "S")
                throw new FormatException("Invalid direction format. Only N and S is accepted.");

            var degreeMatcher = degreeFormatRegex.Match(degree);

            if (!degreeMatcher.Success)
                throw new FormatException("Invalid latitude degree format");

            LatitudeDegree ret = new LatitudeDegree();
            ret.Degree = int.Parse(degreeMatcher.Captures[1].Value);
            ret.Minutes = double.Parse(degreeMatcher.Captures[2].Value);
            ret.Direction = direction == "N" ? LatitudeDegree.DirectionType.North : LatitudeDegree.DirectionType.South;

            return ret;
        }

        [NMEAFieldParser]
        public static LongitudeDegree ParseLongitude(string degree, string direction)
        {
            if (direction != "E" || direction != "W")
                throw new FormatException("Invalid direction format. Only E and W is accepted.");

            var degreeMatcher = degreeFormatRegex.Match(degree);

            if (!degreeMatcher.Success)
                throw new FormatException("Invalid latitude degree format");

            LongitudeDegree ret = new LongitudeDegree();
            ret.Degree = int.Parse(degreeMatcher.Captures[1].Value);
            ret.Minutes = double.Parse(degreeMatcher.Captures[2].Value);
            ret.Direction = direction == "E" ? LongitudeDegree.DirectionType.East : LongitudeDegree.DirectionType.West;

            return ret;
        }

        
    }

    public class NMEAObjectParser
    {
        class NMEAFieldDefinition
        {
            public PropertyInfo PropertyTarget { get; set; }
            public Type TargetType { get; set; }
            public int Index { get; set; }
            public int DependentIndex { get; set; }
        }
        private List<NMEAFieldDefinition> fieldDefinition;
        private Type modelType;

        public NMEAObjectParser(Type type)
        {
            var properties = from t in type.GetProperties()
                             let attr = t.GetCustomAttribute(typeof(NMEAFieldAttribute)) as NMEAFieldAttribute
                             where attr != null
                             select new NMEAFieldDefinition()
                             {
                                 PropertyTarget = t,
                                 TargetType = t.PropertyType,
                                 Index = attr.Index,
                                 DependentIndex = attr.DependentIndex
                             };

            fieldDefinition = properties.ToList();
            modelType = type;
        }

        public GPSModel Parse(string[] values)
        {
            GPSModel retInstance = (GPSModel)modelType.GetConstructor(new Type[0]).Invoke(null);

            foreach(var def in fieldDefinition)
            {
            
            }

            return retInstance;
        }

        
    }
}
