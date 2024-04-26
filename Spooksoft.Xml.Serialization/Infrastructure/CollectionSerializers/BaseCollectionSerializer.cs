using Spooksoft.Xml.Serialization.Attributes;
using Spooksoft.Xml.Serialization.Common;
using Spooksoft.Xml.Serialization.Exceptions;
using Spooksoft.Xml.Serialization.Infrastructure.CollectionSerializers;
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
    internal abstract class BaseCollectionSerializer
    {

        protected object? DeserializeCollection(Type modelType,
            CollectionPropertyInfo propInfo,
            XmlElement propertyElement,
            XmlDocument document,
            IXmlSerializationProvider provider,
            Func<object> typeConstructor,
            Action<object, object?> addItem)
        {
            (_, Dictionary<string, Type> mappings) = GetMappings(propInfo, provider.ConverterProvider, provider.ClassInfoProvider);

            // Null list

            var listNullAttribute = propertyElement.Attributes
                .OfType<XmlAttribute>()
                .FirstOrDefault(a => a.NamespaceURI == Constants.CONTROL_NAMESPACE_URI && a.LocalName == Constants.NIL_ATTRIBUTE);

            if (listNullAttribute != null && listNullAttribute.Value.ToLower() == "true")
            {
                return null;
            }

            // Empty list

            var result = typeConstructor()!;
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

                    addItem(result, null);
                    continue;
                }

                var converter = provider.ConverterProvider.GetConverter(itemType);
                if (converter != null)
                {
                    if (node.ChildNodes.OfType<XmlNode>().Any(cn => cn is not XmlText))
                        throw new XmlSerializationException($"Type convertible to string must be stored as the only text child of item node.");

                    object? item = converter.Deserialize(node.InnerText);
                    addItem(result, item);
                    continue;
                }

                object? deserializedItem = provider.DeserializeExpectedType(itemType, node.Name, node, document);
                addItem(result, deserializedItem);
            }

            return result;
        }

        protected IList? DeserializeIList(Type modelType,
            CollectionPropertyInfo propInfo,
            XmlElement propertyElement,
            XmlDocument document,
            IXmlSerializationProvider provider,
            Func<IList> typeConstructor) =>
            (IList?)DeserializeCollection(modelType, propInfo, propertyElement, document, provider, typeConstructor, (obj, item) => ((IList)obj).Add(item));

        protected (Type collectionType, Dictionary<string, Type> mappings) GetMappings(CollectionPropertyInfo propInfo, IConverterProvider converterProvider, IClassSerializationInfoProvider classInfoProvider)
        {
            Type collectionType;

            if (propInfo.Property.PropertyType.IsGenericType)
            {
                if (propInfo.Property.PropertyType.GetGenericArguments().Length != 1)
                    throw new InvalidOperationException($"{nameof(GetMappings)} method supports only single-type-parameter generic types!");

                collectionType = propInfo.Property.PropertyType.GetGenericArguments()[0];
            }
            else if (propInfo.Property.PropertyType.IsArray)
            {
                collectionType = propInfo.Property.PropertyType.GetElementType()!;
            }
            else
                throw new InvalidOperationException("Unsupported collection type!");

            var mappings = propInfo.CustomTypeMappings;

            if (!mappings.Any())
            {
                var converter = converterProvider.GetConverter(collectionType);
                if (converter != null)
                    mappings.Add("Item", collectionType);
                else
                {
                    var classInfo = classInfoProvider.GetClassInfo(collectionType);
                    mappings.Add(classInfo.XmlRoot, collectionType);
                }
            }

            // Validate mappings

            foreach (var mapping in mappings)
            {
                if (!mapping.Value.IsAssignableTo(collectionType))
                    throw new XmlModelDefinitionException($"Invalid type mapping for {nameof(XmlArrayAttribute)}. Type {mapping.Value.Name} is not assignable to {collectionType.Name}!");
            }

            return (collectionType, mappings);
        }

        protected (Type collectionType, Dictionary<Type, string> reverseMappings) GetReverseMappings(CollectionPropertyInfo propInfo, IConverterProvider converterProvider, IClassSerializationInfoProvider classInfoProvider)
        {
            (var collectionType, var mappings) = GetMappings(propInfo, converterProvider, classInfoProvider);

            Dictionary<Type, string> result = new();
            foreach (var kvp in mappings)
                result[kvp.Value] = kvp.Key;

            return (collectionType, result);
        }

        protected void SerializeAsIEnumerable(object? collection, Type modelType, CollectionPropertyInfo propInfo, XmlElement propertyElement, XmlDocument document, IXmlSerializationProvider provider)
        {
            (var listType, Dictionary<Type, string> mappings) = GetReverseMappings(propInfo, provider.ConverterProvider, provider.ClassInfoProvider);

            // Null collection

            if (collection == null)
            {
                propertyElement.SetAttribute(Constants.NIL_ATTRIBUTE, Constants.CONTROL_NAMESPACE_URI, "true");
                return;
            }

            // Empty collection

            IEnumerable enumerable = (IEnumerable)collection;

            // Items

            foreach (var item in enumerable)
            {
                Type itemType;

                if (item == null)
                    itemType = listType;
                else
                    itemType = item.GetType();

                if (!mappings.TryGetValue(itemType, out string? name) || string.IsNullOrEmpty(name))
                    throw new XmlSerializationException($"Not recognized type on the list (of type {itemType.Name}). Add missing {nameof(XmlArrayItemAttribute)} to the property {propInfo.XmlName} of class {modelType.Name}.\r\nIf you want to keep null values in the collection, remember to add {nameof(XmlArrayItemAttribute)} for the list type (even if it is abstract)");

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
