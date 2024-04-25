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
    internal class IReadOnlyListSerializer : ICollectionSerializer
    {
        private static Dictionary<string, Type> GetMappings(CollectionPropertyInfo propInfo, IConverterProvider converterProvider, IClassSerializationInfoProvider classInfoProvider)
        {
            var genericType = propInfo.Property.PropertyType.GetGenericArguments()[0];

            var mappings = propInfo.CustomTypeMappings;

            if (!mappings.Any())
            {
                var converter = converterProvider.GetConverter(genericType);
                if (converter != null)
                    mappings.Add("Item", genericType);
                else
                {
                    var classInfo = classInfoProvider.GetClassInfo(genericType);
                    mappings.Add(classInfo.XmlRoot, genericType);
                }
            }

            // Validate mappings

            foreach (var mapping in mappings)
            {
                if (!mapping.Value.IsAssignableTo(genericType))
                    throw new XmlModelDefinitionException($"Invalid type mapping for {nameof(XmlArrayAttribute)}. Type {mapping.Value.Name} is not assignable to {genericType.Name}!");
            }

            return mappings;
        }

        private static Dictionary<Type, string> GetReverseMappings(CollectionPropertyInfo propInfo, IConverterProvider converterProvider, IClassSerializationInfoProvider classInfoProvider)
        {
            var mappings = GetMappings(propInfo, converterProvider, classInfoProvider);

            Dictionary<Type, string> result = new();
            foreach (var kvp in mappings)
                result[kvp.Value] = kvp.Key;

            return result;
        }

        public object? Deserialize(Type modelType,
            CollectionPropertyInfo propInfo,
            XmlElement propertyElement,
            XmlDocument document,
            IXmlSerializationProvider provider)
        {
            if (!propInfo.Property.PropertyType.IsGenericType ||
                propInfo.Property.PropertyType.GetGenericTypeDefinition() != typeof(IReadOnlyList<>))
                throw new InvalidOperationException($"${nameof(ListSerializer)} can be called only for property of type List<T>!");

            Dictionary<string, Type> mappings = GetMappings(propInfo, provider.ConverterProvider, provider.ClassInfoProvider);

            // Null list

            var listNullAttribute = propertyElement.Attributes
                .OfType<XmlAttribute>()
                .FirstOrDefault(a => a.NamespaceURI == Constants.CONTROL_NAMESPACE_URI && a.LocalName == Constants.NIL_ATTRIBUTE);

            if (listNullAttribute != null && listNullAttribute.Value.ToLower() == "true")
            {
                return null;
            }

            // Empty list

            var listType = typeof(List<>).MakeGenericType(propInfo.Property.PropertyType.GetGenericArguments()[0]);
            IList result = (IList)Activator.CreateInstance(listType)!;

            if (propertyElement.IsEmpty || propertyElement.ChildNodes.Count == 0)
                return result;

            // Items

            foreach (var node in propertyElement.ChildNodes.OfType<XmlElement>())
            {
                if (!mappings.TryGetValue(node.Name, out var itemType))
                    throw new XmlSerializationException($"Not recognized XML array item (with name {node.Name}). Add missing {nameof(XmlArrayItemAttribute)} to the property {propInfo.XmlName} of class {modelType.Name}.");

                var nullAttribute = node.Attributes
                    .OfType<XmlAttribute>()
                    .FirstOrDefault(a => a.LocalName == Constants.NIL_ATTRIBUTE && a.NamespaceURI == Constants.CONTROL_NAMESPACE_URI);

                if (nullAttribute != null && nullAttribute.Value.ToLower() == "true")
                {
                    result.Add(null);
                    continue;
                }

                var converter = provider.ConverterProvider.GetConverter(itemType);
                if (converter != null)
                {
                    if (node.ChildNodes.OfType<XmlNode>().Any(cn => cn is not XmlText))
                        throw new XmlSerializationException($"Type convertible to string must be stored as the only text child of item node.");

                    object? item = converter.Deserialize(node.InnerText);
                    result.Add(item);
                    continue;
                }

                object? deserializedItem = provider.DeserializeExpectedType(itemType, node.Name, node, document);
                result.Add(deserializedItem);
            }

            return result;
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

            Dictionary<Type, string> mappings = GetReverseMappings(propInfo, provider.ConverterProvider, provider.ClassInfoProvider);

            var listType = propInfo.Property.PropertyType.GetGenericArguments()[0];

            // Null list

            if (collection == null)
            {
                propertyElement.SetAttribute(Constants.NIL_ATTRIBUTE, Constants.CONTROL_NAMESPACE_URI, "true");
                return;
            }

            // Empty list

            IEnumerable list = (IEnumerable)collection;
           
            // Items

            foreach (var item in list)
            {
                Type itemType;

                if (item == null)
                    itemType = listType;
                else
                    itemType = item.GetType();

                if (!mappings.TryGetValue(itemType, out string? name) || string.IsNullOrEmpty(name))
                    throw new XmlSerializationException($"Not recognized type on the list (of type {itemType.Name}). Add missing {nameof(XmlArrayItemAttribute)} to the property {propInfo.XmlName} of class {modelType.Name}.");

                if (item == null)
                {
                    var itemElement = document.CreateElement(name);
                    itemElement.SetAttribute(Constants.NIL_ATTRIBUTE, Constants.CONTROL_NAMESPACE_URI, "true");
                    propertyElement.AppendChild(itemElement);

                    continue;
                }

                var converter = provider.ConverterProvider.GetConverter(itemType);
                if (converter != null)
                {
                    var itemElement = document.CreateElement(name);
                    itemElement.InnerText = converter.Serialize(item);
                    propertyElement.AppendChild(itemElement);

                    continue;
                }

                var serializedItemElement = provider.SerializeObjectToElement(item, itemType, name, document);
                propertyElement.AppendChild(serializedItemElement);
            }
        }
    }
}
