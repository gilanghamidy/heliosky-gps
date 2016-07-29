/*   UBXModelBase.cs
 *   Copyright (C) 2016 Gilang M. Hamidy (gilang.hamidy@gmail.com)
 *   
 *   This file is part of Heliosky.IoT.GPS
 * 
 *   Heliosky.IoT.GPS is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU Lesser General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   Heliosky.IoT.GPS is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU Lesser General Public License for more details.
 *
 *   You should have received a copy of the GNU Lesser General Public License
 *   along with Heliosky.IoT.GPS.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Heliosky.IoT.GPS
{
    internal struct UBXFieldDefinition
    {
        public PropertyInfo Property { get; set; }
        public int Size { get; set; }
        public bool ListType { get; set; }
        public int? ListIndexRef { get; set; }
    }


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
            public short? PayloadSize { get; set; }
            public byte[] PollMessage { get; set; }
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

        private class UBXStaticMessage : UBXModelBase
        {
            public UBXStaticMessage(byte[] staticMessage)
            {
                base.staticMessage = staticMessage;
            }
        }

        static UBXModelBase()
        {
            Assembly thisAssembly = typeof(UBXModelBase).GetTypeInfo().Assembly;

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
                    if(!property.ListType)
                    {
                        // If property is not a list type, parse normally using its underlying type
                        property.Property.SetValue(retVal, reader.Read(property.Property.PropertyType));
                    }
                    else
                    {
                        // If property is a list type, infer the type content
                        var typeInfoOfPropertyType = property.Property.PropertyType.GetTypeInfo();
                        var theStructureType = typeInfoOfPropertyType.GenericTypeArguments[0]; // Get the T of IEnumerable<T>

                        // Get the size of the structure
                        var structureSize = UBXStructureMapper.PayloadSizeOf(theStructureType);

                        // Get the item count
                        var itemCount = Convert.ToInt32(ubxType.PropertyMap[property.ListIndexRef.Value].Property.GetValue(retVal));

                        // Construct list of it
                        var theList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(theStructureType));


                        for(int i = 0; i < itemCount; i++)
                        {
                            var buf = reader.ReadBytes(structureSize);
                            theList.Add(UBXStructureMapper.TryParse(theStructureType, buf));
                        }

                        // Set the value to property
                        property.Property.SetValue(retVal, theList);
                    }
                }

                return retVal;
            }
            catch(KeyNotFoundException)
            {
                throw new UnknownMessageException(string.Format("Unknown message with Class: {0}, MessageID: {1}", classId, messageId));
            }
            catch(NotSupportedException ex)
            {
                throw new UnknownMessageException(string.Format("Failed to parse Class: {0}, MessageID: {1}", classId, messageId), ex);
            }
        }

        public static bool IsConfigMessage(UBXModelBase message)
        {
            var attribute = message.GetType().GetTypeInfo().GetCustomAttribute<UBXConfigAttribute>();
            if (attribute != null)
                return true;
            else
                return false;
        }
        
        internal static UBXMessageAttribute GetMessageAttribute(UBXModelBase message)
        {
            return message.GetType().GetTypeInfo().GetCustomAttribute<UBXMessageAttribute>();
        }

        public static bool KnownMessageType(byte classId, byte messageId)
        {
            return parsableTypeIndex.Keys.Contains(new UBXMessageIndex(classId, messageId));
        }

        private static UBXMessageDefinition GenerateDefinition(Type t, UBXMessageAttribute metadata)
        {
            var typeInfo = t.GetTypeInfo();

            var listOfDeclaredProperties = from prop in TypeExtensions.GetProperties(t, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                           let attr = prop.GetCustomAttribute<UBXFieldAttribute>()
                                           let info = prop.PropertyType.GetTypeInfo()
                                           let listType = info.IsGenericType && typeof(IEnumerable<>) == info.GetGenericTypeDefinition()
                                           let listIndex = listType ? prop.GetCustomAttribute<UBXListAttribute>().ItemCountField : (int?)null
                                           where attr != null
                                           orderby attr.Index
                                           select new UBXFieldDefinition()
                                           {
                                               Property = prop,
                                               Size = listType ? 0 : Marshal.SizeOf(prop.PropertyType),
                                               ListType = listType,
                                               ListIndexRef = listIndex
                                           };

            return new UBXMessageDefinition()
            {
                PropertyMap = listOfDeclaredProperties.ToList(),
                PayloadSize = listOfDeclaredProperties.Aggregate<UBXFieldDefinition, bool>(false, (x, y) => x || y.ListType) ? null : (short?)listOfDeclaredProperties.Sum(x => x.Size),
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
        private byte[] staticMessage;

        protected UBXModelBase()
        {
            var realType = this.GetType();

            if (realType != typeof(UBXStaticMessage))
            { 

                var typeInfo = realType.GetTypeInfo();
                var attr = typeInfo.GetCustomAttribute<UBXMessageAttribute>();

                if (attr == null)
                    throw new NotSupportedException(String.Format("This class ({0}) does not declare UBXMessageAttribute, thus cannot be instantiated.", typeInfo.FullName));

                this.classId = attr.ClassID;
                this.messageId = attr.MessageID;
                this.staticMessage = null;
            }
        }

        public byte[] ToBinaryData()
        {
            if (staticMessage != null)
                return staticMessage;

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

        public static UBXModelBase GetPollMessage<T>() where T : UBXModelBase
        {
            var propertyDef = propertyMapper[typeof(T)];

            if ((propertyDef.Metadata.Type & MessageType.Poll) == 0)
                throw new NotSupportedException(String.Format("The type {0} cannot be used as poll request.", typeof(T).FullName));

            if (propertyDef.PollMessage != null)
                return new UBXStaticMessage(propertyDef.PollMessage);

            MemoryStream str = new MemoryStream();
            str.WriteByte(propertyDef.Metadata.ClassID);
            str.WriteByte(propertyDef.Metadata.MessageID);
            str.WriteByte(0);
            str.WriteByte(0);
            str.Flush();
            byte[] data = str.ToArray();
            var checksum = GetChecksum(data);

            str.Dispose();
            str = new MemoryStream();
            var wrt = new BinaryWriter(str);

            wrt.Write(Header1); // Header 1
            wrt.Write(Header2); // Header 2
            wrt.Write(data, 0, data.Length); // ClassID MessageID Payload
            wrt.Write(checksum); // Checksum

            // Store locally
            propertyDef.PollMessage = str.ToArray();
            return new UBXStaticMessage(propertyDef.PollMessage);
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

    public static class UBXStructureMapper
    {
        private class UBXStructureDefinition
        {
            public Type MessageClass { get; set; }
            public List<UBXFieldDefinition> PropertyMap { get; set; }
            public short PayloadSize { get; set; }
        }

        private static Dictionary<Type, UBXStructureDefinition> structureDictionary;

        static UBXStructureMapper()
        {
            Assembly thisAssembly = typeof(UBXStructureMapper).GetTypeInfo().Assembly;

            var items = from t in thisAssembly.GetTypes()
                        let attr = t.GetTypeInfo().GetCustomAttribute(typeof(UBXStructureAttribute)) as UBXStructureAttribute
                        where attr != null
                        let definition = GenerateDefinition(t)
                        select definition;

            structureDictionary = items.ToDictionary(k => k.MessageClass, v => v);
        }

        private static UBXStructureDefinition GenerateDefinition(Type t)
        {
            var typeInfo = t.GetTypeInfo();

            var listOfDeclaredProperties = from prop in TypeExtensions.GetProperties(t, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                           let attr = prop.GetCustomAttribute<UBXFieldAttribute>()
                                           where attr != null
                                           orderby attr.Index
                                           select new UBXFieldDefinition() { Property = prop, Size = Marshal.SizeOf(prop.PropertyType) };

            return new UBXStructureDefinition()
            {
                PropertyMap = listOfDeclaredProperties.ToList(),
                PayloadSize = (short)listOfDeclaredProperties.Sum(x => x.Size),
                MessageClass = t
            };
        }

        internal static int PayloadSizeOf(Type t)
        {
            try
            {
                return structureDictionary[t].PayloadSize;
            }
            catch(KeyNotFoundException)
            {
                throw new NotSupportedException(String.Format("Cannot find type {0} as UBXStructure type", t.FullName));
            }
        }

        internal static object TryParse(Type t, byte[] payload)
        {
            try
            {
                var definition = structureDictionary[t];

                BinaryReader reader = new BinaryReader(new MemoryStream(payload));

                object retVal = Activator.CreateInstance(t);

                foreach (var property in definition.PropertyMap)
                {
                    property.Property.SetValue(retVal, reader.Read(property.Property.PropertyType));
                }

                return retVal;
            }
            catch (KeyNotFoundException)
            {
                throw new NotSupportedException(String.Format("Cannot find type {0} as UBXStructure type", t.FullName));
            }
        }
    }
}
