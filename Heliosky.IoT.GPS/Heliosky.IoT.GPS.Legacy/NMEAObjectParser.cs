using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
