using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Attributes
{
    [Obsolete("Please use SpkXmlMapValueAttribute instead to avoid clash with System.Xml.Serialization namespace.")]
    public class XmlMapValueAttribute : SpkXmlMapValueAttribute
    {
        public XmlMapValueAttribute(string name, Type type) : base(name, type)
        {
        }
    }
}
