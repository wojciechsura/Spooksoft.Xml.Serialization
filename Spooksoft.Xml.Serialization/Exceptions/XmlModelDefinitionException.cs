using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Exceptions
{
    [Serializable]
    public class XmlModelDefinitionException : Exception
    {
        public XmlModelDefinitionException() { }
        public XmlModelDefinitionException(string message) : base(message) { }
        public XmlModelDefinitionException(string message, Exception inner) : base(message, inner) { }
    }
}
