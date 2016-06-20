using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Heliosky.IoT.GPS
{
    public abstract class UBXModels
    {
        private static Dictionary<Type, List<PropertyInfo>> propertyMapper = new Dictionary<Type, List<PropertyInfo>>();

        private byte classId;
        private byte messageId;

        protected UBXModels()
        {
            var realType = this.GetType();
            var typeInfo = realType.GetTypeInfo();
            var attr = typeInfo.GetCustomAttribute<UBXMessageAttribute>();

            if (attr == null)
                throw new NotSupportedException(String.Format("This class ({0}) does not declare UBXMessageAttribute, thus cannot be instantiated.", typeInfo.FullName));

            MapProperty(realType);

            this.classId = attr.ClassID;
            this.messageId = attr.MessageID;
        }

        private static void MapProperty(Type t)
        {
            if (propertyMapper.Keys.Contains(t))
                return;

            var typeInfo = t.GetTypeInfo();

            var listOfDeclaredProperties = from prop in typeInfo.DeclaredProperties
                                           let attr = prop.GetCustomAttribute<UBXFieldAttribute>()
                                           where attr != null
                                           orderby attr.Index
                                           select prop;

            propertyMapper.Add(t, listOfDeclaredProperties.ToList());
        }

        public byte[] ToBinaryData()
        {
            MemoryStream str = new MemoryStream();

            str.WriteByte(classId);
            str.WriteByte(messageId);

            BinaryWriter wrt = new BinaryWriter(str);
            

            return null;
        }
    }
}
