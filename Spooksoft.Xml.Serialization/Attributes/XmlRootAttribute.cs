using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Attributes
{
    [Obsolete("Please use SpkXmlRootAttribute instead to avoid clash with System.Xml.Serialization namespace.")]
    public class XmlRootAttribute : SpkXmlRootAttribute
    {
        public XmlRootAttribute(string name) : base(name)
        {
        }
    }
}
