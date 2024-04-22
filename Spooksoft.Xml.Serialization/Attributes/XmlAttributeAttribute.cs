using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class XmlAttributeAttribute : Attribute
    {
        public XmlAttributeAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
