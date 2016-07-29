/*   NMEAObjectParser.cs
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

namespace Heliosky.IoT.GPS.Legacy
{
    public class NMEAObjectParser
    {
        class NMEAFieldDefinition
        {
            public PropertyInfo PropertyTarget { get; set; }
            public Type TargetType { get; set; }
            public int Index { get; set; }
            public int? DependentIndex { get; set; }
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
                                 Index = attr.Index - 1,
                                 DependentIndex = attr.DependentIndex.HasValue ? attr.DependentIndex - 1 : null
                             };

            fieldDefinition = properties.ToList();
            modelType = type;
        }

        public GPSModel Parse(string[] values)
        {
            GPSModel retInstance = (GPSModel)modelType.GetConstructor(new Type[0]).Invoke(null);

            foreach (var def in fieldDefinition)
            {
                if (values[def.Index] != null && values[def.Index].Length != 0)
                {
                    try
                    {
                        if (def.TargetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            var internalType = def.TargetType.GenericTypeArguments[0];
                            def.PropertyTarget.SetValue(retInstance, NMEAFormat.ParseValue(internalType, values[def.Index], def.DependentIndex != null ? values[def.DependentIndex.Value] : null));
                        }
                        else
                        {
                            throw new Exception();
                        }
                    }
                    catch(Exception)
                    {
                        def.PropertyTarget.SetValue(retInstance, NMEAFormat.ParseValue(def.TargetType, values[def.Index], def.DependentIndex != null ? values[def.DependentIndex.Value] : null));
                    }
                }
                
            }

            return retInstance;
        }


    }
}
