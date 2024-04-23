using Spooksoft.Xml.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Models
{
    internal abstract class BaseClassInfo
    {
        protected BaseClassInfo(Type type, XmlRootAttribute? rootAttribute)
        {
            Type = type;
            RootAttribute = rootAttribute;
        }

        public Type Type { get; }
        public XmlRootAttribute? RootAttribute { get; }

        public string XmlRoot => RootAttribute?.Name ?? Type.Name;
    }
}
