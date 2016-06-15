using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heliosky.IoT.GPS
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
