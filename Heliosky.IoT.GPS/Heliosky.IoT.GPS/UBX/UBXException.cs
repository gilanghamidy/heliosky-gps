using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heliosky.IoT.GPS.UBX
{
    public class UBXException : Exception
    {
        public UBXException() { }
        public UBXException(string message) : base(message) { }
        public UBXException(string message, Exception inner) : base(message, inner) { }
    }

    public class InvalidMessageHeaderException : UBXException
    {
        public InvalidMessageHeaderException() { }
        public InvalidMessageHeaderException(string message) : base(message) { }
        public InvalidMessageHeaderException(string message, Exception inner) : base(message, inner) { }
    }

    public class UnknownMessageException : UBXException
    {
        public UnknownMessageException() { }
        public UnknownMessageException(string message) : base(message) { }
        public UnknownMessageException(string message, Exception inner) : base(message, inner) { }
    }

    public class InvalidChecksumException : UBXException
    {
        public InvalidChecksumException() { }
        public InvalidChecksumException(string message) : base(message) { }
        public InvalidChecksumException(string message, Exception inner) : base(message, inner) { }
    }

}
