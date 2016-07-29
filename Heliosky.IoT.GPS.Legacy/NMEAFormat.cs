/*   NMEAFormat.cs
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
using System.Text.RegularExpressions;
using System.Reflection;

namespace Heliosky.IoT.GPS.Legacy
{
    public static class NMEAFormat
    {
        private static Regex timeFormatRegex;
        private static Regex degreeFormatRegex;
        private static Dictionary<Type, MethodInfo> fieldParserMethod;

        static NMEAFormat()
        {
            timeFormatRegex = new Regex(@"([0-2][0-9])([0-5][0-9])([0-5][0-9])(\.([0-9]{2}))?$");
            degreeFormatRegex = new Regex(@"([0-9]{2,3})([0-9]{2}\.[0-9]{0,5})");

            var internalParser = from method in typeof(NMEAFormat).GetTypeInfo().DeclaredMethods
                                 let attr = method.GetCustomAttribute(typeof(NMEAFieldParserAttribute)) as NMEAFieldParserAttribute
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
            if (!fieldParserMethod.Keys.Contains(targetType))
                throw new InvalidOperationException(String.Format("No parser can parse {0}", targetType.FullName));

            var parserMethod = fieldParserMethod[targetType];

            int paramCount = parserMethod.GetParameters().Length;

            if (paramCount == 1)
            {
                // One parameter
                return parserMethod.Invoke(null, new object[] { value });
            }
            else
            {
                // Two parameter
                return parserMethod.Invoke(null, new object[] { value, dependent });
            }
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
                hour: int.Parse(timeMatcher.Groups[1].Value),
                minute: int.Parse(timeMatcher.Groups[2].Value),
                second: int.Parse(timeMatcher.Groups[3].Value),
                millisecond: (timeMatcher.Groups[5].Success ? int.Parse(timeMatcher.Groups[5].Value) * 10 : 0));

            return ret;
        }

        [NMEAFieldParser]
        public static LatitudeDegree ParseLatitude(string degree, string direction)
        {
            if (direction != "N" && direction != "S")
                throw new FormatException("Invalid direction format. Only N and S is accepted.");

            var degreeMatcher = degreeFormatRegex.Match(degree);

            if (!degreeMatcher.Success)
                throw new FormatException("Invalid latitude degree format");

            LatitudeDegree ret = new LatitudeDegree();
            ret.Degree = int.Parse(degreeMatcher.Groups[1].Value);
            ret.Minutes = double.Parse(degreeMatcher.Groups[2].Value);
            ret.Direction = direction == "N" ? LatitudeDegree.DirectionType.North : LatitudeDegree.DirectionType.South;

            return ret;
        }

        [NMEAFieldParser]
        public static LongitudeDegree ParseLongitude(string degree, string direction)
        {
            if (direction != "E" && direction != "W")
                throw new FormatException("Invalid direction format. Only E and W is accepted.");

            var degreeMatcher = degreeFormatRegex.Match(degree);

            if (!degreeMatcher.Success)
                throw new FormatException("Invalid latitude degree format");

            LongitudeDegree ret = new LongitudeDegree();
            ret.Degree = int.Parse(degreeMatcher.Groups[1].Value);
            ret.Minutes = double.Parse(degreeMatcher.Groups[2].Value);
            ret.Direction = direction == "E" ? LongitudeDegree.DirectionType.East : LongitudeDegree.DirectionType.West;

            return ret;
        }


    }
}
