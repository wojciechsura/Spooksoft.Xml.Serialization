using Spooksoft.Xml.Serialization.Models.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Spooksoft.Xml.Serialization
{
    internal interface IMapSerializer
    {
        void Serialize(object? map,
            Type modelType,
            MapPropertyInfo propInfo,
            XmlElement propertyElement,
            XmlDocument document,
            IXmlSerializationProvider provider);

        object? Deserialize(Type modelType,
            MapPropertyInfo propInfo,
            XmlElement propertyElement,
            XmlDocument document,
            IXmlSerializationProvider provider);
    }
}
