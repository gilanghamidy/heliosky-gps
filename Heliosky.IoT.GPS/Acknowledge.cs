/*   Acknowledge.cs
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
    
    public class AcknowledgeBase : UBXModelBase
    {
        [UBXField(0)]
        public byte ClassID { get; set; }

        [UBXField(1)]
        public byte MessageID { get; set; }
    }

    [UBXMessage(0x05, 0x01, MessageType.Receive)]
    public class Acknowledge : AcknowledgeBase
    {
        public override string ToString()
        {
            return String.Format("ACK => ClassID {0} MessageID {1}", ClassID, MessageID);
        }
    }

    [UBXMessage(0x05, 0x00, MessageType.Receive)]
    public class NotAcknowledge : AcknowledgeBase
    {
        public override string ToString()
        {
            return String.Format("NOT-ACK => ClassID {0} MessageID {1}", ClassID, MessageID);
        }
    }
}
