using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Attributes
{
    [Obsolete("Please use SpkXmlAttributeAttribute instead to avoid clash with System.Xml.Serialization namespace.")]
    public class XmlAttributeAttribute : SpkXmlAttributeAttribute
    {
        public XmlAttributeAttribute(string name) : base(name)
        {
        }
    }
}
