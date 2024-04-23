using Spooksoft.Xml.Serialization.Attributes;
using Spooksoft.Xml.Serialization.Models.Construction;
using Spooksoft.Xml.Serialization.Models.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Models
{
    internal class SerializableClassInfo : BaseClassInfo
    {
        public SerializableClassInfo(XmlRootAttribute? rootAttribute,
            BaseClassConstructionInfo construction,
            IReadOnlyList<BasePropertyInfo> properties)
        {
            RootAttribute = rootAttribute;
            ConstructionInfo = construction;
            Properties = properties;
        }

        public XmlRootAttribute? RootAttribute { get; }

        public BaseClassConstructionInfo ConstructionInfo { get; }
        public IReadOnlyList<BasePropertyInfo> Properties { get; }
    }
}
