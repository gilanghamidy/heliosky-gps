using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Heliosky.IoT.GPS.UBX
{
    public abstract class UBXModelBase
    {
        private static Dictionary<Type, UBXMessageDefinition> propertyMapper = new Dictionary<Type, UBXMessageDefinition>();

        private struct UBXMessageDefinition
        {
            public List<PropertyInfo> PropertyMap { get; set; }
            public short PayloadSize { get; set; }
        }

        private byte classId;
        private byte messageId;

        protected UBXModelBase()
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
                                           select new { Property = prop, Size = Marshal.SizeOf(prop.PropertyType) };

            propertyMapper.Add(t, new UBXMessageDefinition() {
                PropertyMap = listOfDeclaredProperties.Select(x=> x.Property).ToList(),
                PayloadSize = (short)listOfDeclaredProperties.Sum(x => x.Size)
            });
        }

        public byte[] ToBinaryData()
        {
            MemoryStream str = new MemoryStream();
            var propertyDef = propertyMapper[this.GetType()];
            str.WriteByte(classId);
            str.WriteByte(messageId);
            

            BinaryWriter wrt = new BinaryWriter(str);
            wrt.Write((short)propertyDef.PayloadSize);

            foreach (var prop in propertyDef.PropertyMap)
            {
                wrt.Write(prop.PropertyType, prop.GetValue(this));
            }

            wrt.Flush();
            byte[] data = str.ToArray();
            var checksum = GetChecksum(data);

            str.Dispose();
            wrt.Dispose();
            str = new MemoryStream();
            wrt = new BinaryWriter(str);

            wrt.Write((byte)0xB5); // Header 1
            wrt.Write((byte)0x62); // Header 2
            wrt.Write(data, 0, data.Length); // ClassID MessageID Payload
            wrt.Write(checksum); // Checksum
            
            return str.ToArray();
        }


        private static ushort GetChecksum(byte[] payload)
        {
            unchecked
            {
                uint crc_a = 0;
                uint crc_b = 0;
                if (payload.Length > 0)
                {
                    for(int i = 0; i < payload.Length; i++)
                    { 
                        crc_a += payload[i];
                        crc_b += crc_a;
                    }
                    crc_a &= 0xff;
                    crc_b &= 0xff;
                }
                return (ushort)(crc_a | (crc_b << 8));
            }
        }
    }

    public static class BinaryWriterHelper
    {
        private static Dictionary<Type, MethodInfo> methodList;

        static BinaryWriterHelper()
        {
            var typeMapping = from method in typeof(BinaryWriter).GetTypeInfo().DeclaredMethods
                              let parameters = method.GetParameters()
                              where parameters.Length == 1 && method.Name == "Write"
                              select new { Parameter = parameters[0], Method = method };

            methodList = typeMapping.ToDictionary(k => k.Parameter.ParameterType, v => v.Method);
        }

        public static void Write(this BinaryWriter wrt, Type paramType, object value)
        {
            if(!methodList.Keys.Contains(paramType))
                throw new NotSupportedException(String.Format("Cannot serialize type {0} using BinaryWriter", paramType.FullName));

            var method = methodList[paramType];

            method.Invoke(wrt, new object[] { value });
        }
    }
}
