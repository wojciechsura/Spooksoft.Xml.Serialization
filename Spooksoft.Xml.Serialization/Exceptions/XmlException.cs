using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Exceptions
{

    [Serializable]
    public class XmlException : Exception
    {
        protected XmlException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }

        public XmlException() { }
        public XmlException(string message) : base(message) { }
        public XmlException(string message, Exception inner) : base(message, inner) { }
    }    
}
