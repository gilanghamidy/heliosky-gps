using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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

    class NMEAParser
    {
        private Dictionary<string, NMEAObjectParser> parserList;

        public NMEAParser()
        {
            

            Assembly thisAssembly = typeof(NMEAParser).GetTypeInfo().Assembly;

            var items = from t in thisAssembly.GetTypes()
                        let attr = t.GetTypeInfo().GetCustomAttribute(typeof(NMEAStringAttribute))
                        where attr != null
                        select attr as NMEAStringAttribute;


            this.parserList = items.ToDictionary(k => k.Keyword, v => v.Parser);

        }

        GPSModel Parse(string input)
        {
            return null;
        }
    }

    interface NMEAObjectParser
    {
        GPSModel Parse(string[] values);
    }
}
