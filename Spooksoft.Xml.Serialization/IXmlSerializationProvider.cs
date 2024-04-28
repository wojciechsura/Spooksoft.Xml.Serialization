using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Spooksoft.Xml.Serialization
{
    internal interface IXmlSerializationProvider
    { 
        void SerializePropertyValue(Type modelType, XmlElement propertyElement, object? value, ITypeMappingProperty property, XmlDocument document);
        object? DeserializePropertyValue(Type modelType, XmlElement itemElement, ITypeMappingProperty property, XmlDocument document);
    }
}
