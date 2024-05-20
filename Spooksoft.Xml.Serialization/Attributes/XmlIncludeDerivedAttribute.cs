using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Attributes
{
    [Obsolete("Please use SpkXmlIncludeDerivedAttribute instead to avoid clash with System.Xml.Serialization namespace.")]
    public class XmlIncludeDerivedAttribute : SpkXmlIncludeDerivedAttribute
    {
        public XmlIncludeDerivedAttribute(string name, Type type) : base(name, type)
        {
        }
    }
}
