using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class XmlIncludeDerivedAttribute : Attribute
    {
        public XmlIncludeDerivedAttribute(string name, Type type)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; }
        public Type Type { get; }
    }
}
