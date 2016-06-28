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
        public const byte Header1 = 0xB5;
        public const byte Header2 = 0x62;

        private static Dictionary<Type, UBXMessageDefinition> propertyMapper;
        private static Dictionary<UBXMessageIndex, UBXMessageDefinition> parsableTypeIndex;

        private class UBXMessageDefinition
        {
            public Type MessageClass { get; set; }
            public UBXMessageAttribute Metadata { get; set; }
            public List<UBXFieldDefinition> PropertyMap { get; set; }
            public short PayloadSize { get; set; }
        }

        private struct UBXFieldDefinition
        {
            public PropertyInfo Property { get; set; }
            public int Size { get; set; }
        }

        private struct UBXMessageIndex
        {
            public short PayloadSize { get; set; }
            public byte ClassID { get; set; }
            public byte MessageID { get; set; }

            public UBXMessageIndex(byte classId, byte messageId)
            {
                this.ClassID = classId;
                this.MessageID = messageId;
                PayloadSize = 0;
            }

            public override bool Equals(object obj)
            {
                try
                {
                    var that = (UBXMessageIndex)obj;

                    return this.ClassID == that.ClassID
                                && this.MessageID == that.MessageID;
                }
                catch(Exception)
                {
                    return false;
                }
            }

            public override int GetHashCode()
            {
                return  this.ClassID * this.MessageID;
            }
        }

        static UBXModelBase()
        {
            Assembly thisAssembly = typeof(NMEAParser).GetTypeInfo().Assembly;

            var items = from t in thisAssembly.GetTypes()
                        let attr = t.GetTypeInfo().GetCustomAttribute(typeof(UBXMessageAttribute)) as UBXMessageAttribute
                        where attr != null
                        let definition = GenerateDefinition(t, attr)
                        select definition;

            propertyMapper = items.ToDictionary(k => k.MessageClass, v => v);
            parsableTypeIndex = items.Where(x => (x.Metadata.Type & MessageType.Receive) != 0).ToDictionary(k => new UBXMessageIndex(k.Metadata.ClassID, k.Metadata.MessageID), v => v);

        }

        public static UBXModelBase TryParse(byte[] payload)
        {
            if (payload[0] != Header1 || payload[1] != Header2)
                throw new InvalidMessageHeaderException();

            byte classId = payload[2];
            byte messageId = payload[3];
            int messageLength = payload[4] | (payload[5] << 8);

            try
            {
                var ubxType = parsableTypeIndex[new UBXMessageIndex(classId, messageId)];

                ushort expectedChecksum = (ushort)((payload[payload.Length - 2]) | (payload[payload.Length - 1] << 8));
                ushort computedChecksum = GetChecksum(payload, 2, payload.Length - 2);

                if (expectedChecksum != computedChecksum)
                    throw new InvalidChecksumException(String.Format("Checksum expected {0}, computed {1}", expectedChecksum, computedChecksum));

                BinaryReader reader = new BinaryReader(new MemoryStream(payload, 6, messageLength));

                UBXModelBase retVal = (UBXModelBase)Activator.CreateInstance(ubxType.MessageClass);

                foreach(var property in ubxType.PropertyMap)
                {
                    property.Property.SetValue(retVal, reader.Read(property.Property.PropertyType));
                }

                return retVal;
            }
            catch(KeyNotFoundException)
            {
                throw new UnknownMessageException(string.Format("Unknown message with Class: {0}, MessageID: {1}", classId, messageId));
            }
        }

        private static UBXMessageDefinition GenerateDefinition(Type t, UBXMessageAttribute metadata)
        {
            var typeInfo = t.GetTypeInfo();

            var listOfDeclaredProperties = from prop in TypeExtensions.GetProperties(t, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                           let attr = prop.GetCustomAttribute<UBXFieldAttribute>()
                                           where attr != null
                                           orderby attr.Index
                                           select new UBXFieldDefinition() { Property = prop, Size = Marshal.SizeOf(prop.PropertyType) };

            return new UBXMessageDefinition()
            {
                PropertyMap = listOfDeclaredProperties.ToList(),
                PayloadSize = (short)listOfDeclaredProperties.Sum(x => x.Size),
                MessageClass = t,
                Metadata = metadata
            };
        }

        private static ushort GetChecksum(byte[] payload)
        {
            return GetChecksum(payload, 0, payload.Length);
        }

        private static ushort GetChecksum(byte[] payload, int indexStart, int length)
        {
            unchecked
            {
                uint crc_a = 0;
                uint crc_b = 0;
                if (payload.Length > 0)
                {
                    for (int i = indexStart; i < length; i++)
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

        private byte classId;
        private byte messageId;

        protected UBXModelBase()
        {
            var realType = this.GetType();
            var typeInfo = realType.GetTypeInfo();
            var attr = typeInfo.GetCustomAttribute<UBXMessageAttribute>();

            if (attr == null)
                throw new NotSupportedException(String.Format("This class ({0}) does not declare UBXMessageAttribute, thus cannot be instantiated.", typeInfo.FullName));

            this.classId = attr.ClassID;
            this.messageId = attr.MessageID;
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
                wrt.Write(prop.Property.PropertyType, prop.Property.GetValue(this));
            }

            wrt.Flush();
            byte[] data = str.ToArray();
            var checksum = GetChecksum(data);

            str.Dispose();
            wrt.Dispose();
            str = new MemoryStream();
            wrt = new BinaryWriter(str);

            wrt.Write(Header1); // Header 1
            wrt.Write(Header2); // Header 2
            wrt.Write(data, 0, data.Length); // ClassID MessageID Payload
            wrt.Write(checksum); // Checksum
            
            return str.ToArray();
        }


    }

    public static class BinaryReaderHelper
    {
        private static Dictionary<Type, MethodInfo> methodList;

        static BinaryReaderHelper()
        {
            var typeMapping = from method in typeof(BinaryReader).GetTypeInfo().DeclaredMethods
                              let parameters = method.GetParameters()
                              where parameters.Length == 0 
                                 && method.Name.Contains("Read") 
                                 && method.Name != "Read"
                                 && method.IsPublic == true
                              select new { ReturnType = method.ReturnType, Method = method };

            methodList = typeMapping.ToDictionary(k => k.ReturnType, v => v.Method);
        }

        public static object Read(this BinaryReader rdr, Type targetType)
        {
            if (!methodList.Keys.Contains(targetType))
                throw new NotSupportedException(String.Format("Cannot deserialize type {0} using BinaryReader", targetType.FullName));

            var method = methodList[targetType];

            return method.Invoke(rdr, null);
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
