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
        public XmlException() { }
        public XmlException(string message) : base(message) { }
        public XmlException(string message, Exception inner) : base(message, inner) { }
    }    
}
