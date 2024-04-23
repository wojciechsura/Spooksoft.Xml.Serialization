using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Exceptions
{
    [Serializable]
    public class XmlSerializationException : XmlException
    {
        public XmlSerializationException() { }
        public XmlSerializationException(string message) : base(message) { }
        public XmlSerializationException(string message, Exception inner) : base(message, inner) { }
        protected XmlSerializationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
