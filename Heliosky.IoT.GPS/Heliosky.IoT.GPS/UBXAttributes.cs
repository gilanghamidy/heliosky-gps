using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heliosky.IoT.GPS
{
    [Flags]
    enum MessageType
    {
        Send = 0x1,
        Receive = 0x2,
        Poll = 0x4
    }

    [System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    sealed class UBXMessageAttribute : Attribute
    {
        public UBXMessageAttribute(byte classId, byte messageId, MessageType type)
        {
            ClassID = classId;
            MessageID = messageId;
            Type = type;
        }

        public byte ClassID { get; private set; }
        public byte MessageID { get; private set; }
        public MessageType Type { get; private set; }
    }

    [System.AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    sealed class UBXConfigAttribute : Attribute
    {

    }

    [System.AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    sealed class UBXFieldAttribute : Attribute
    {
        public UBXFieldAttribute(int index)
        {
            Index = index;
        }

        public int Index { get; private set; }
    }

    [System.AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    sealed class UBXStructureAttribute : Attribute
    {
        public UBXStructureAttribute()
        {

        }
    }

    [System.AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    sealed class UBXListAttribute : Attribute
    {

        public UBXListAttribute(int itemCountField)
        {
            this.ItemCountField = itemCountField;
        }

        public int ItemCountField { get; private set; }
    }
}
