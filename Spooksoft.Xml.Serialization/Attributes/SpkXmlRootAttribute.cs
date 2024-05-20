using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class SpkXmlRootAttribute : Attribute
    {
        public SpkXmlRootAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
