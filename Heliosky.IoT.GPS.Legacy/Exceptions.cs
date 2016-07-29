/*   Exceptions.cs
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

namespace Heliosky.IoT.GPS.Legacy
{
    public class NMEAException : Exception
    {
        public NMEAException() { }
        public NMEAException(string message) : base(message) { }
        public NMEAException(string message, Exception inner) : base(message, inner) { }
    }

    public class InvalidChecksumException : NMEAException
    {
        public InvalidChecksumException() { }
        public InvalidChecksumException(string message) : base(message) { }
        public InvalidChecksumException(string message, Exception inner) : base(message, inner) { }
    }

    public class UnknownMessageException : NMEAException
    {
        public UnknownMessageException() { }
        public UnknownMessageException(string message) : base(message) { }
        public UnknownMessageException(string message, Exception inner) : base(message, inner) { }
    }

}
