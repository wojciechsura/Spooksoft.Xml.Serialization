using Microsoft.Win32;
using Spooksoft.Xml.Serialization.Attributes;
using Spooksoft.Xml.Serialization.Common;
using Spooksoft.Xml.Serialization.Exceptions;
using Spooksoft.Xml.Serialization.Models.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Spooksoft.Xml.Serialization.Infrastructure.MapSerializers
{
    internal class DictionarySerializer : IMapSerializer
    {
        public object? Deserialize(Type modelType, MapPropertyInfo propInfo, XmlElement propertyElement, XmlDocument document, IXmlSerializationProvider provider)
        {
            var mapNullAttribute = propertyElement.Attributes
                .OfType<XmlAttribute>()
                .FirstOrDefault(a => a.NamespaceURI == Constants.CONTROL_NAMESPACE_URI && a.LocalName == Constants.NIL_ATTRIBUTE);

            if (mapNullAttribute != null && mapNullAttribute.Value.ToLower() == "true")
            {
                return null;
            }

            // Map

            Type resultType = typeof(Dictionary<,>).MakeGenericType(propInfo.KeyType, propInfo.ValueType);
            var resultAddMethod = resultType.GetMethod("Add")!;

            var result = Activator.CreateInstance(resultType);
            if (propertyElement.IsEmpty || propertyElement.ChildNodes.Count == 0)
                return result;

            // Items

            foreach (var itemNode in propertyElement.ChildNodes.OfType<XmlElement>())
            {
                var keyNode = itemNode.ChildNodes.OfType<XmlElement>().FirstOrDefault(c => c.Name == "Key");
                var valueNode = itemNode.ChildNodes.OfType<XmlElement>().FirstOrDefault(c => c.Name == "Value");

                if (keyNode == null)
                    throw new XmlSerializationException($"Map item element is missing Key child element! Property {propInfo.Property.Name} of type {modelType.Name}");
                if (valueNode == null)
                    throw new XmlSerializationException($"Map item element is missing Value child element! Property {propInfo.Property.Name} of type {modelType.Name}");

                var keyDataNode = keyNode.ChildNodes.OfType<XmlElement>().FirstOrDefault();
                var valueDataNode = valueNode.ChildNodes.OfType<XmlElement>().FirstOrDefault();

                if (keyDataNode == null)
                    throw new XmlSerializationException($"Map item's Key element is missing its child element! Property {propInfo.Property.Name} of type {modelType.Name}");
                if (valueDataNode == null)
                    throw new XmlSerializationException($"Map item's Value element is missing its child element! Property {propInfo.Property.Name} of type {modelType.Name}");

                object? key = provider.DeserializePropertyValue(modelType, keyDataNode, propInfo.KeyMappingProperty, document);
                object? value = provider.DeserializePropertyValue(modelType, valueDataNode, propInfo.ValueMappingProperty, document);

                resultAddMethod.Invoke(result, new[] { key, value });
            }

            return result;
        }

        public void Serialize(object? map, Type modelType, MapPropertyInfo propInfo, XmlElement propertyElement, XmlDocument document, IXmlSerializationProvider provider)
        {
            // Null map

            if (map == null)
            {
                propertyElement.SetAttribute(Constants.NIL_ATTRIBUTE, Constants.CONTROL_NAMESPACE_URI, "true");
                return;
            }

            // Enumerable

            IEnumerable enumerable = (IEnumerable)map;

            // Item type

            var kvpType = typeof(KeyValuePair<,>).MakeGenericType(propInfo.KeyType, propInfo.ValueType);
            var keyProp = kvpType.GetProperty("Key")!;
            var valueProp = kvpType.GetProperty("Value")!;

            // Items

            foreach (var kvp in enumerable)
            {
                var itemElement = document.CreateElement("Item");
                propertyElement.AppendChild(itemElement);

                var keyElement = document.CreateElement("Key");
                provider.SerializePropertyValue(modelType, keyElement, keyProp.GetValue(kvp), propInfo.KeyMappingProperty, document);
                itemElement.AppendChild(keyElement);

                var valueElement = document.CreateElement("Value");
                provider.SerializePropertyValue(modelType, valueElement, valueProp.GetValue(kvp), propInfo.ValueMappingProperty, document);
                itemElement.AppendChild(valueElement);
            }
        }
    }
}
