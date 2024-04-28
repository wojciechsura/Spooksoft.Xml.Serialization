using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Exceptions
{
    public class XmlInvalidTypeMappingsException : Exception
    {
        public XmlInvalidTypeMappingsException() { }
        public XmlInvalidTypeMappingsException(string message) : base(message) { }
        public XmlInvalidTypeMappingsException(string message, Exception inner) : base(message, inner) { }
    }
}
