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
        protected BaseClassInfo(Type type, SpkXmlRootAttribute? rootAttribute)
        {
            Type = type;
            RootAttribute = rootAttribute;
        }

        public Type Type { get; }
        public SpkXmlRootAttribute? RootAttribute { get; }

        public string XmlRoot => RootAttribute?.Name ?? Type.Name;
    }
}
