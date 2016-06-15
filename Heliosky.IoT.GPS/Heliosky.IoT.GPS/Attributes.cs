using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heliosky.IoT.GPS
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
            this.DependentIndex = null;
        }

        // This is a positional argument
        public NMEAFieldAttribute(int index, int dependentIndex)
        {
            this.Index = index;
            this.DependentIndex = dependentIndex;
        }

        public int Index { get; private set; }
        public int? DependentIndex { get; private set; }
    }

    [System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    sealed class NMEAFieldParserAttribute : Attribute
    {
        // This is a positional argument
        public NMEAFieldParserAttribute()
        {

        }
    }
}
