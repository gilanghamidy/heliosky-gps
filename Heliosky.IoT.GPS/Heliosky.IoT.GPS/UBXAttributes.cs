/*   UBXAttributes.cs
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
