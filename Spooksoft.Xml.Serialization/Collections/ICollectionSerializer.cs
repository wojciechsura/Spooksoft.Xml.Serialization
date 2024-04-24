using Spooksoft.Xml.Serialization.Models.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Spooksoft.Xml.Serialization.Collections
{
    internal interface ICollectionSerializer
    {
        void Serialize(object? collection, CollectionPropertyInfo propInfo, XmlElement propertyElement);
        object? Deserialize(CollectionPropertyInfo propInfo, XmlElement propertyElement);
    }
}
