using Spooksoft.Xml.Serialization.Models.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Spooksoft.Xml.Serialization
{
    internal interface ICollectionSerializer
    {
        void Serialize(object? collection, 
            Type modelType,
            CollectionPropertyInfo propInfo, 
            XmlElement propertyElement, 
            XmlDocument document, 
            IXmlSerializationProvider provider);

        object? Deserialize(Type modelType, 
            CollectionPropertyInfo propInfo, 
            XmlElement propertyElement, 
            XmlDocument document, 
            IXmlSerializationProvider provider);
    }
}
