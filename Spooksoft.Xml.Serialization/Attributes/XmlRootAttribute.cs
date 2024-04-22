using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class XmlRootAttribute : Attribute
    {
        public XmlRootAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
