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
    internal class DictionarySerializer : BaseMapSerializer, IMapSerializer
    {
        public object? Deserialize(Type modelType, MapPropertyInfo propInfo, XmlElement propertyElement, XmlDocument document, IXmlSerializationProvider provider)
        {
            (var keyType, var valueType, var keyMappings, var valueMappings) = GetMappings(propInfo, provider.ConverterProvider, provider.ClassInfoProvider);

            var mapNullAttribute = propertyElement.Attributes
                .OfType<XmlAttribute>()
                .FirstOrDefault(a => a.NamespaceURI == Constants.CONTROL_NAMESPACE_URI && a.LocalName == Constants.NIL_ATTRIBUTE);

            if (mapNullAttribute != null && mapNullAttribute.Value.ToLower() == "true")
            {
                return null;
            }

            // Map

            Type resultType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
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

                object? GetValue(Dictionary<string, Type> pairItemMappings, XmlElement pairItemDataNode, string pairItemName, Type attribute)
                {
                    if (!pairItemMappings.TryGetValue(pairItemDataNode.Name, out var pairItemType))
                        throw new XmlSerializationException($"Not recognized XML {pairItemName} item (with name {pairItemDataNode.Name}). Add missing {attribute.Name} to the to the property {propInfo.XmlName} of class {modelType.Name}.");

                    var nullAttribute = pairItemDataNode.Attributes
                        .OfType<XmlAttribute>()
                        .FirstOrDefault(a => a.LocalName == Constants.NIL_ATTRIBUTE && a.NamespaceURI == Constants.CONTROL_NAMESPACE_URI);

                    if (nullAttribute != null && nullAttribute.Value.ToLower() == "true")
                    {
                        return null;
                    }

                    var converter = provider.ConverterProvider.GetConverter(pairItemType);
                    if (converter != null)
                    {
                        if (pairItemDataNode.ChildNodes.OfType<XmlNode>().Any(cn => cn is not XmlText))
                            throw new XmlSerializationException($"Type convertible to string must be stored as the only text of item node.");

                        object? item = converter.Deserialize(pairItemDataNode.InnerText);
                        return item;
                    }

                    object? deserializedItem = provider.DeserializeExpectedType(pairItemType, pairItemDataNode.Name, pairItemDataNode, document);
                    return deserializedItem;
                }

                var key = GetValue(keyMappings, keyDataNode, "key", typeof(XmlMapKeyAttribute));
                var value = GetValue(valueMappings, valueDataNode, "value", typeof(XmlMapValueAttribute));

                resultAddMethod.Invoke(result, new[] { key, value });
            }

            return result;
        }

        public void Serialize(object? map, Type modelType, MapPropertyInfo propInfo, XmlElement propertyElement, XmlDocument document, IXmlSerializationProvider provider)
        {
            (var keyType, var valueType, var keyMappings, var valueMappings) = GetReverseMappings(propInfo, provider.ConverterProvider, provider.ClassInfoProvider);

            // Null map

            if (map == null)
            {
                propertyElement.SetAttribute(Constants.NIL_ATTRIBUTE, Constants.CONTROL_NAMESPACE_URI, "true");
                return;
            }

            // Enumerable

            IEnumerable enumerable = (IEnumerable)map;

            // Item type

            var kvpType = typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType);
            var keyProp = kvpType.GetProperty("Key")!;
            var valueProp = kvpType.GetProperty("Value")!;

            // Items

            foreach (var kvp in enumerable)
            {
                (Type, object?, string) GetTypeForProperty(PropertyInfo prop, Type defaultType, Dictionary<Type, string> mappings, string propertyName, Type attributeType)
                {
                    var itemValue = prop.GetValue(kvp);
                    Type itemPropertyType;
                    if (itemValue == null)
                        itemPropertyType = defaultType;
                    else
                        itemPropertyType = itemValue.GetType();

                    if (!mappings.TryGetValue(itemPropertyType, out string? propertyElementName) || string.IsNullOrEmpty(propertyElementName))
                        throw new XmlSerializationException($"Not recognized type of {propertyName} in the map (of type {itemPropertyType.Name}). Add missing {attributeType.Name} to the property {propInfo.XmlName} of class {modelType.Name}.\r\nIf you want to use null {propertyName}s in the map, remember to add {nameof(XmlMapKeyAttribute)} for the map {propertyName} type (even if it is abstract)");

                    return (itemPropertyType, itemValue, propertyElementName);
                }

                // Key/Value types and values

                (var itemKeyType, var key, string keyName) = GetTypeForProperty(keyProp, keyType, keyMappings, "key", typeof(XmlMapKeyAttribute));
                (var itemValueType, var value, string valueName) = GetTypeForProperty(valueProp, valueType, valueMappings, "value", typeof(XmlMapValueAttribute));

                // Item

                var itemElement = document.CreateElement("Item");

                // Key and Value elements

                void AppendProperty(string pairItemElementName, string pairItemDataElementName, object? pairItemValue, Type pairItemPropertyType)
                {
                    var pairItemElement = document.CreateElement(pairItemElementName);
                    itemElement.AppendChild(pairItemElement);

                    if (pairItemValue == null)
                    {
                        var propertyDataElement = document.CreateElement(pairItemDataElementName);
                        propertyDataElement.SetAttribute(Constants.NIL_ATTRIBUTE, Constants.CONTROL_NAMESPACE_URI, "true");
                    }
                    else
                    {
                        var converter = provider.ConverterProvider.GetConverter(pairItemPropertyType);
                        if (converter != null)
                        {
                            var propertyDataElement = document.CreateElement(pairItemDataElementName);
                            pairItemElement.AppendChild(propertyDataElement);
                            propertyDataElement.InnerText = converter.Serialize(pairItemValue);
                        }
                        else
                        {
                            var serializedDataElement = provider.SerializeObjectToElement(pairItemValue, pairItemPropertyType, keyName, document);
                            pairItemElement.AppendChild(serializedDataElement);
                        }
                    }
                }

                AppendProperty("Key", keyName, key, itemKeyType);
                AppendProperty("Value", valueName, value, itemValueType);

                propertyElement.AppendChild(itemElement);
            }
        }
    }
}
