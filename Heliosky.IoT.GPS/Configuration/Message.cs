/*   Message.cs
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
using System.Reflection;

namespace Heliosky.IoT.GPS.Configuration
{
    [UBXConfig]
    [UBXMessage(0x06, 0x01, MessageType.Send | MessageType.Receive)]
    public class Message : UBXModelBase
    {
        [UBXField(0)]
        public byte ClassID { get; private set; }

        [UBXField(1)]
        public byte MessageID { get; private set; }

        [UBXField(2)]
        public byte Rate { get; set; }

        private Message()
        {

        }

        public static Message GetConfigurationForType<T>() where T : UBXModelBase
        {
            var attr = typeof(T).GetTypeInfo().GetCustomAttribute<UBXMessageAttribute>();

            if (attr == null)
                throw new NotSupportedException("UBXMessageAttribute not found on specified type.");

            if ((attr.Type & MessageType.Receive) == 0)
                throw new NotSupportedException("Only receive-type message that can be configured.");

            return new Message()
            {
                ClassID = attr.ClassID,
                MessageID = attr.MessageID,
                Rate = 1
            };
        }
    }
}
