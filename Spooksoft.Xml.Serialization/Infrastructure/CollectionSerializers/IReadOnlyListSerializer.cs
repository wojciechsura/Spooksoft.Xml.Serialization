using Spooksoft.Xml.Serialization.Attributes;
using Spooksoft.Xml.Serialization.Common;
using Spooksoft.Xml.Serialization.Exceptions;
using Spooksoft.Xml.Serialization.Models.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Spooksoft.Xml.Serialization.Infrastructure.CollectionSerializers
{
    internal class IReadOnlyListSerializer : BaseCollectionSerializer, ICollectionSerializer
    {
        public object? Deserialize(Type modelType,
            CollectionPropertyInfo propInfo,
            XmlElement propertyElement,
            XmlDocument document,
            IXmlSerializationProvider provider)
        {
            IList activator()
            {
                var listType = typeof(List<>).MakeGenericType(propInfo.Property.PropertyType.GetGenericArguments()[0]);
                return (IList)Activator.CreateInstance(listType)!;
            }

            if (!propInfo.Property.PropertyType.IsGenericType ||
                propInfo.Property.PropertyType.GetGenericTypeDefinition() != typeof(IReadOnlyList<>))
                throw new InvalidOperationException($"${nameof(ListSerializer)} can be called only for property of type List<T>!");

            return DeserializeIList(modelType, propInfo, propertyElement, document, provider, activator);            
        }

        public void Serialize(object? collection,
            Type modelType,
            CollectionPropertyInfo propInfo,
            XmlElement propertyElement,
            XmlDocument document,
            IXmlSerializationProvider provider)
        {
            if (!propInfo.Property.PropertyType.IsGenericType ||
                propInfo.Property.PropertyType.GetGenericTypeDefinition() != typeof(IReadOnlyList<>))
                throw new InvalidOperationException($"${nameof(IReadOnlyListSerializer)} can be called only for property of type IReadOnlyList<T>!");

            SerializeAsIEnumerable(collection, modelType, propInfo, propertyElement, document, provider);
        }
    }
}
