/*   Attributes.cs
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

namespace Heliosky.IoT.GPS.Legacy
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
