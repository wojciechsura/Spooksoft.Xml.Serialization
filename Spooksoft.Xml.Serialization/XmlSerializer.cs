using Spooksoft.Xml.Serialization.Attributes;
using Spooksoft.Xml.Serialization.Common;
using Spooksoft.Xml.Serialization.Exceptions;
using Spooksoft.Xml.Serialization.Infrastructure;
using Spooksoft.Xml.Serialization.Models;
using Spooksoft.Xml.Serialization.Models.Construction;
using Spooksoft.Xml.Serialization.Models.Properties;
using Spooksoft.Xml.Serialization.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Spooksoft.Xml.Serialization
{
    public class XmlSerializer : IXmlSerializationProvider
    {
        // Private types ------------------------------------------------------

        private sealed class PropertyValue
        {
            public PropertyValue(BasePropertyInfo property, object? value)
            {
                if (property.ConstructorParameterIndex != null)
                    throw new InvalidOperationException($"{nameof(SimplePropertyInfo)} passed to {nameof(PropertyValue)} must not have {nameof(SimplePropertyInfo.ConstructorParameterIndex)} set!");

                Property = property;
                Value = value;
            }

            public BasePropertyInfo Property { get; }
            public object? Value { get; }
        }

        private sealed class ConstructorParameterValue
        {
            public ConstructorParameterValue(BasePropertyInfo property, object? value)
            {
                if (property.ConstructorParameterIndex == null)
                    throw new InvalidOperationException($"{nameof(SimplePropertyInfo)} passed to {nameof(ConstructorParameterValue)} must have {nameof(SimplePropertyInfo.ConstructorParameterIndex)} set!");

                Property = property;
                Value = value;
            }

            public BasePropertyInfo Property { get; }
            public object? Value { get; }
        }

        private sealed class ClassDeserializationData
        {
            public ClassDeserializationData(ClassSerializationInfo classSerializationInfo)
            {
                ClassSerializationInfo = classSerializationInfo;

                if (classSerializationInfo.ConstructionInfo is ParameteredClassConstructionInfo paramederedConstruction)
                {
                    ConstructorParameterValues = new ConstructorParameterValue?[paramederedConstruction.ConstructorParameters.Count];
                }
                else
                {
                    ConstructorParameterValues = Array.Empty<ConstructorParameterValue?>();
                }

                PropertyValues = new();
                IsNull = false;
            }

            public ClassSerializationInfo ClassSerializationInfo { get; }
            public bool IsNull { get; set; }
            public List<PropertyValue> PropertyValues { get; }
            public ConstructorParameterValue?[] ConstructorParameterValues { get; }
        }

        // Private fields -----------------------------------------------------

        private static readonly Dictionary<Type, BaseClassInfo> sharedTypeCache = new();
        private static readonly object sharedTypeCacheLock = new();

        private readonly IClassSerializationInfoProvider classSerializationInfoProvider;
        private readonly IConverterProvider converterProvider;
        private readonly ICollectionSerializerProvider collectionSerializerProvider;
        private readonly MapSerializerProvider mapSerializerProvider;
        private readonly XmlSerializerConfig config;

        // Private methods ----------------------------------------------------

        private void SerializePropertyValue(Type modelType, XmlElement propertyElement, object? item, ITypeMappingProperty property, XmlDocument document)
        {
            Type itemType;

            if (item == null)
                itemType = property.BaseType;
            else
                itemType = item.GetType();

            if (!property.SerializationMappings.TryGetValue(itemType, out string? name) || string.IsNullOrEmpty(name))
                throw new XmlSerializationException($"Not recognized type {itemType.Name}. Add missing {property.CustomTypeAttribute.Name} to the property {property.Name} of class {modelType.Name}.\r\nIf you want to store null values, remember to add {property.CustomTypeAttribute.Name} for the type {property.BaseType}.");

            if (item == null)
            {
                var itemElement = document.CreateElement(name);
                itemElement.SetAttribute(Constants.NIL_ATTRIBUTE, Constants.CONTROL_NAMESPACE_URI, "true");
                propertyElement.AppendChild(itemElement);
            }
            else
            {
                var converter = converterProvider.GetConverter(itemType);
                if (converter != null)
                {
                    var itemElement = document.CreateElement(name);
                    propertyElement.AppendChild(itemElement);
                    itemElement.InnerText = converter.Serialize(item);
                }
                else
                {
                    var serializedItemElement = SerializeObjectToElement(item, itemType, name, document);
                    propertyElement.AppendChild(serializedItemElement);
                }
            }
        }

        private object? DeserializePropertyValue(Type modelType, XmlElement itemElement, ITypeMappingProperty property, XmlDocument document)
        {
            if (!property.DeserializationMappings.TryGetValue(itemElement.Name, out var itemType))
                throw new XmlSerializationException($"Not recognized item (with name {itemElement.Name}). Add missing {property.CustomTypeAttribute.Name} to the property {property.Name} of class {modelType.Name}.");

            var nullAttribute = itemElement.Attributes
                .OfType<XmlAttribute>()
                .FirstOrDefault(a => a.LocalName == Constants.NIL_ATTRIBUTE && a.NamespaceURI == Constants.CONTROL_NAMESPACE_URI);

            if (nullAttribute != null && nullAttribute.Value.ToLower() == "true")
            {
                return null;
            }

            var converter = converterProvider.GetConverter(itemType);
            if (converter != null)
            {
                if (itemElement.ChildNodes.OfType<XmlNode>().Any(cn => cn is not XmlText))
                    throw new XmlSerializationException($"Type convertible to string must be stored as the only text child of item node. Type: {itemType.Name}, property {property.Name} of class {modelType.Name}.");

                object? item = converter.Deserialize(itemElement.InnerText);
                return item;
            }

            object? deserializedItem = DeserializeExpectedType(itemType, itemElement.Name, itemElement, document);
            return deserializedItem;
        }

        private void SerializePropertyToAttribute(object model, 
            BasePropertyInfo property, 
            XmlElement classElement,
            XmlDocument document)
        {
            if (property is SimplePropertyInfo)
            {
                // Get property type
                var propertyType = property.Property.PropertyType;

                // Get value of the property
                object? value = property.Property.GetValue(model);

                // Find converter
                var converter = converterProvider.GetConverter(propertyType) ??
                    throw new XmlSerializationException($"Cannot find converter for type {propertyType.Name} to serialize property {property.Property.Name} of class {model.GetType().Name}");

                // Serialize
                var attribute = document.CreateAttribute(property.XmlName);
                attribute.Value = converter.Serialize(value);
                classElement.Attributes.Append(attribute);
            }
            else
                throw new InvalidOperationException($"Can not serialize property other than {nameof(SimplePropertyInfo)} to an attribue!");
        }

        private void SerializePropertyToElement(object model,
            BasePropertyInfo property,
            XmlElement classElement,
            XmlDocument document)
        {
            if (property is SimplePropertyInfo simpleProp)
            {
                if (simpleProp.UsedCustomMappings)
                {
                    // Get property value
                    var value = simpleProp.Property.GetValue(model);

                    var propertyElement = document.CreateElement(simpleProp.XmlName);
                    SerializePropertyValue(model.GetType(), propertyElement, value, simpleProp, document);
                    classElement.AppendChild(propertyElement);
                }
                else
                {
                    // Get property type
                    var propertyType = simpleProp.Property.PropertyType;

                    // Get property value
                    var value = simpleProp.Property.GetValue(model);

                    // Find converter
                    var converter = converterProvider.GetConverter(propertyType);
                    if (converter != null)
                    {
                        // If there is a converter for this type, serialize the
                        // property as string                    

                        var propertyElement = document.CreateElement(simpleProp.XmlName);
                        propertyElement.InnerText = converter.Serialize(value);
                        classElement.AppendChild(propertyElement);
                    }
                    else
                    {
                        // If it is a class, serialize recursively into
                        // an element

                        var propertyElement = document.CreateElement(simpleProp.XmlName);
                        var subElement = SerializeObjectToElement(value, propertyType, classElement.OwnerDocument);
                        propertyElement.AppendChild(subElement);
                        classElement.AppendChild(propertyElement);
                    }
                }                
            }
            else if (property is BinaryPropertyInfo binaryProp)
            {
                // Get property type
                var propertyType = binaryProp.Property.PropertyType;

                if (propertyType != typeof(byte[]))
                    throw new XmlModelDefinitionException($"An {nameof(XmlBinaryAttribute)} attribute can be attached only to a property of type byte[]");

                // Get property value

                var value = (byte[]?)binaryProp.Property.GetValue(model);

                var propertyElement = document.CreateElement(binaryProp.XmlName);
                classElement.AppendChild(propertyElement);

                if (value == null)
                {
                    propertyElement.SetAttribute(Constants.NIL_ATTRIBUTE, Constants.CONTROL_NAMESPACE_URI, "true");
                }
                else
                {
                    propertyElement.InnerText = Convert.ToBase64String(value);
                }
            }
            else if (property is CollectionPropertyInfo collectionProp)
            {
                // Get property type
                var propertyType = collectionProp.Property.PropertyType;

                // Get property value
                var value = collectionProp.Property.GetValue(model);

                // Get collection serializer
                var serializer = collectionSerializerProvider.GetCollectionSerializer(propertyType) ??
                    throw new XmlSerializationException($"Cannot serialize property {collectionProp.Property.Name} of class {model.GetType().Name}. No suitable collection serializer found for type {propertyType.Name}");

                // Serialize
                var propertyElement = document.CreateElement(collectionProp.XmlName);
                serializer.Serialize(value, model.GetType(), collectionProp, propertyElement, document, this);
                classElement.AppendChild(propertyElement);
            }
            else if (property is MapPropertyInfo mapProp)
            {
                // Get property type
                var propertyType = mapProp.Property.PropertyType;

                // Get property value
                var value = mapProp.Property.GetValue(model);

                // Get map serializer
                var serializer = mapSerializerProvider.GetMapSerializer(propertyType) ??
                    throw new XmlSerializationException($"Cannot serialize property {mapProp.Property.Name} of class {model.GetType().Name}. No suitable collection serializer found for type {propertyType.Name}");

                // Serialize
                var propertyElement = document.CreateElement(mapProp.XmlName);
                serializer.Serialize(value, model.GetType(), mapProp, propertyElement, document, this);
                classElement.AppendChild(propertyElement);
            }
            else
                throw new InvalidOperationException("Unsupported property info!");
        }

        private XmlElement SerializeObjectToElement(object? model, Type modelType, XmlDocument document, bool isRoot = false)
        {
            ArgumentNullException.ThrowIfNull(modelType);
            ArgumentNullException.ThrowIfNull(document);

            var classInfo = classSerializationInfoProvider.GetClassInfo(modelType);

            return SerializeObjectToElement(model, modelType, classInfo.XmlRoot, document, isRoot);
        }

        private XmlElement SerializeObjectToElement(object? model, Type modelType, string elementName, XmlDocument document, bool isRoot = false)
        {
            ArgumentNullException.ThrowIfNull(modelType);
            ArgumentNullException.ThrowIfNull(document);

            var classInfo = classSerializationInfoProvider.GetClassInfo(modelType);

            // If we are serializing the root element, the namespaces
            // must be set up here. It is required for a corner case when
            // the root element is null - it then needs to have xmlns:xsi
            // namespace set already for the xsi:nil attribute.
            var result = document.CreateElement(elementName);
            if (isRoot)
            {
                result.SetAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
            }

            if (model == null)
            {
                result.SetAttribute(Constants.NIL_ATTRIBUTE, Constants.CONTROL_NAMESPACE_URI, "true");
            }
            else
            {                
                switch (classInfo)
                {
                    case ClassCustomSerializationInfo:
                        {
                            // User serializes contents of this class on his own

                            var serializable = (IXmlSerializable)model;
                            serializable.Write(result);
                            break;
                        }
                    case ClassSerializationInfo serializableClassInfo:
                        {
                            // Class is serialized automatically

                            foreach (var prop in serializableClassInfo.Properties)
                            {
                                switch (prop.XmlPlacement)
                                {
                                    case XmlPlacement.Attribute:
                                        SerializePropertyToAttribute(model, prop, result, document);
                                        break;
                                    case XmlPlacement.Element:
                                        SerializePropertyToElement(model, prop, result, document);
                                        break;
                                    default:
                                        throw new InvalidOperationException("Unsupported XML placement!");
                                }
                            }

                            break;
                        }
                    default:
                        throw new InvalidOperationException("Unsupported class info!");
                }
            }

            return result;
        }

        private void Serialize(object? model, Type modelType, XmlDocument document)
        {
            ArgumentNullException.ThrowIfNull(modelType);
            ArgumentNullException.ThrowIfNull(document);

            var element = SerializeObjectToElement(model, modelType, document, true);
            
            document.AppendChild(element);
        }

        private object ConstructDeserializedClass(ClassSerializationInfo serializableClassInfo, 
            ClassDeserializationData data, 
            XmlDocument document)
        {
            object? result = null;

            switch (serializableClassInfo.ConstructionInfo)
            {
                case ParameterlessClassConstructionInfo:
                    {
                        result = Activator.CreateInstance(serializableClassInfo.Type)!;

                        break;
                    }
                case ParameteredClassConstructionInfo parameteredConstruction:
                    {
                        // Ensure that there are no missing values
                        // Fill those with default values if user wants to
                        for (int i = 0; i < parameteredConstruction.ConstructorParameters.Count; i++)
                        {
                            // Note that data.ConstructorParameterValues[i] being null
                            // means that there was no parameter supplied. Null value
                            // will still be contained in the ParameterValue instance.

                            if (data.ConstructorParameterValues[i] == null)
                            {
                                if (config.ReplaceMissingCtorParamsWithDefaultValues)
                                {
                                    var propertyInfo = serializableClassInfo.Properties
                                        .Single(p => p.ConstructorParameterIndex == i);

                                    var paramType = parameteredConstruction.ConstructorParameters[i].MatchingProperty.PropertyType;
                                    object? defaultValue = paramType.IsValueType ? Activator.CreateInstance(paramType) : null;

                                    var replacementValue = new ConstructorParameterValue(propertyInfo, defaultValue);
                                    data.ConstructorParameterValues[i] = replacementValue;
                                }
                                else
                                    throw new XmlSerializationException($"Failed to find value for parameter {i} of the constructor of type {serializableClassInfo.Type.Name}");
                            }
                        }

                        // Now instantiate class

                        result = Activator.CreateInstance(serializableClassInfo.Type, 
                            data.ConstructorParameterValues.Select(pv => pv!.Value).ToArray())!;

                        break;
                    }
                default:
                    throw new InvalidOperationException("Unsupported construction info!");
            }

            return result;
        }

        private void DeserializeAttributes(ClassDeserializationData data, XmlElement element, XmlDocument document)
        {
            foreach (XmlAttribute attribute in element.Attributes)
            {
                // Skip namespace definition attributes

                if (attribute.Name == "xmlns" || attribute.Name.StartsWith("xmlns:"))
                    continue;

                // Skip any namespaced attributes - they are handled differently

                if (!string.IsNullOrEmpty(attribute.NamespaceURI))
                    continue;

                // Find property stored in an attribute

                var propInfo = data.ClassSerializationInfo.Properties
                    .FirstOrDefault(pi => pi.XmlName == attribute.Name && pi.XmlPlacement == Types.XmlPlacement.Attribute);

                if (propInfo == null)
                {
                    if (config.ErrorOnNotRecognizedProperties)
                        throw new XmlSerializationException($"Not recognized attribute {attribute.Name} for type {data.ClassSerializationInfo.Type.Name}");
                    else
                        continue;
                }

                // Only simple properties can be stored in attributes

                if (propInfo is not SimplePropertyInfo simplePropInfo)
                    throw new XmlSerializationException($"Property {propInfo.Property.Name} of class {data.ClassSerializationInfo.Type.Name} is not a simple property (a collection?), which means it can be placed only in an element.");

                // Get converter for found property type

                var converter = converterProvider.GetConverter(simplePropInfo.Property.PropertyType) ??
                    throw new XmlSerializationException($"No converter found for type {simplePropInfo.Property.PropertyType.Name} to deserialize property {simplePropInfo.Property.Name} of class {data.ClassSerializationInfo.Type.Name}");

                // Deserialize value of the property

                object? value = null;
                try
                {
                    value = converter.Deserialize(attribute.Value);
                }
                catch (Exception e)
                {
                    if (config.ErrorOnFailedPropertyDeserialization)
                        throw new XmlSerializationException($"Failed to deserialize attribute value {attribute.Value} to {simplePropInfo.Property.PropertyType.Name}", e);
                    else
                        continue;
                }

                // Store deserialized value, depending on kind of property

                if (simplePropInfo.ConstructorParameterIndex != null)
                {
                    data.ConstructorParameterValues[simplePropInfo.ConstructorParameterIndex.Value] = new ConstructorParameterValue(simplePropInfo, value);
                }
                else
                {
                    data.PropertyValues.Add(new PropertyValue(simplePropInfo, value));
                }
            }
        }

        private void DeserializeElements(ClassDeserializationData data, XmlElement element, XmlDocument document)
        {
            foreach (var subElement in element.ChildNodes.OfType<XmlElement>())
            {
                // Find property stored in an element

                var propInfo = data.ClassSerializationInfo.Properties.FirstOrDefault(pi => pi.XmlName == subElement.Name && pi.XmlPlacement == Types.XmlPlacement.Element);
                if (propInfo == null)
                {
                    if (config.ErrorOnNotRecognizedProperties)
                        throw new XmlSerializationException($"Not recognized element {subElement.Name} for type {data.ClassSerializationInfo.Type.Name}");
                    else
                    {
                        // Skip contents of the current property                                    
                        continue;
                    }
                }

                object? value = null;

                if (propInfo is SimplePropertyInfo simplePropInfo)
                {
                    if (simplePropInfo.UsedCustomMappings)
                    {
                        if (subElement.ChildNodes.OfType<XmlElement>().Count() != 1)
                            throw new XmlSerializationException($"Node representing property {propInfo.Property.Name} of type {data.ClassSerializationInfo.Type.Name} should have exactly one child element!");

                        var dataElement = subElement.ChildNodes.OfType<XmlElement>().Single();

                        value = DeserializePropertyValue(data.ClassSerializationInfo.Type, dataElement, simplePropInfo, document);
                    }
                    else
                    {
                        // Try to get a converter for property type

                        var converter = converterProvider.GetConverter(propInfo.Property.PropertyType);

                        if (converter != null)
                        {
                            // For convertible types, values are stored as strings
                            // in the element's InnerText
                            // 
                            string? contents = subElement.InnerText ?? string.Empty;

                            // Try to deserialize

                            try
                            {
                                value = converter.Deserialize(contents);
                            }
                            catch (Exception e)
                            {
                                if (config.ErrorOnFailedPropertyDeserialization)
                                    throw new XmlSerializationException($"Failed to deserialize element value {subElement.Value} to {propInfo.Property.PropertyType.Name}", e);
                                else
                                    continue;
                            }
                        }
                        else
                        {
                            // No available converter means sub-element

                            var propertyType = propInfo.Property.PropertyType;

                            var children = subElement.ChildNodes.OfType<XmlElement>().ToArray();
                            if (children.Length != 1)
                                throw new XmlSerializationException($"Expected a single sub-element for node {subElement.Name} representing value of property {propInfo.Property.Name} of class {data.ClassSerializationInfo.Type.Name}");

                            var child = children[0];

                            value = DeserializeExpectedType(propertyType, child.Name, child, document);
                        }
                    }
                }
                else if (propInfo is BinaryPropertyInfo binaryProp)
                {
                    // Check for null
                    var nullAttribute = subElement.Attributes
                        .OfType<XmlAttribute>()
                        .FirstOrDefault(a => a.LocalName == Constants.NIL_ATTRIBUTE && a.NamespaceURI == Constants.CONTROL_NAMESPACE_URI);

                    if (nullAttribute != null && nullAttribute.Value.ToLower() == "true")
                    {
                        value = null;
                    }
                    else
                    {
                        if (subElement.ChildNodes.OfType<XmlNode>().Any(cn => cn is not XmlText))
                            throw new XmlSerializationException($"Element representing a binary node cannot contain any other sub-elements than text! Property {propInfo.Property.Name} of class {data.ClassSerializationInfo.Type.Name}");

                        if (string.IsNullOrEmpty(subElement.InnerText))
                        {
                            value = Array.Empty<byte>();
                        }
                        else
                        {
                            value = Convert.FromBase64String(subElement.InnerText);
                        }
                    }
                }
                else if (propInfo is CollectionPropertyInfo collectionProp)
                {
                    // Try to get a serializer for collection

                    var serializer = collectionSerializerProvider.GetCollectionSerializer(collectionProp.Property.PropertyType) ?? 
                        throw new XmlSerializationException($"Cannot deserialize property {collectionProp.Property.Name} of class {data.ClassSerializationInfo.Type.Name}. No suitable collection serializer found for type {collectionProp.Property.PropertyType.Name}");

                    // Deserialize collection using found serializer

                    value = serializer.Deserialize(data.ClassSerializationInfo.Type, collectionProp, subElement, document, this);
                }
                else if (propInfo is MapPropertyInfo mapProp)
                {
                    // Try to get a serializer for map

                    var serializer = mapSerializerProvider.GetMapSerializer(mapProp.Property.PropertyType) ??
                        throw new XmlSerializationException($"Cannot deserialize property {mapProp.Property.Name} of class {data.ClassSerializationInfo.Type.Name}. No suitable map serializer found for type {mapProp.Property.PropertyType.Name}");

                    // Deserialize map using found serializer

                    value = serializer.Deserialize(data.ClassSerializationInfo.Type, mapProp, subElement, document, this);
                }
                else
                    throw new InvalidOperationException("Unsupported property info!");

                // Store deserialized value, depending on kind of property

                if (propInfo.ConstructorParameterIndex != null)
                {
                    data.ConstructorParameterValues[propInfo.ConstructorParameterIndex.Value] = new ConstructorParameterValue(propInfo, value);
                }
                else
                {
                    data.PropertyValues.Add(new PropertyValue(propInfo, value));
                }
            }
        }

        private void DeserializeNullInformation(ClassDeserializationData data, XmlElement element, XmlDocument document)
        {
            var attribute = element.Attributes.OfType<XmlAttribute>()
                .FirstOrDefault(attr => attr.NamespaceURI == Constants.CONTROL_NAMESPACE_URI && attr.LocalName == Constants.NIL_ATTRIBUTE);
            if (attribute != null && attribute.Value.ToLower() == "true")
            {
                data.IsNull = true;
            }            
        }

        private object? DeserializeClass(Type type, XmlElement element, ClassSerializationInfo serializableClassInfo, XmlDocument document)
        {
            // Collect information from attributes and sub-elements

            ClassDeserializationData data = new(serializableClassInfo);
            DeserializeAttributes(data, element, document);
            DeserializeElements(data, element, document);
            DeserializeNullInformation(data, element, document);

            // Collected all information about the deserialized object
            // Time to instantiate it and actually fill with deserialized
            // data.

            // If xsi:nil="true" attribute is present, instance was null 
            // during serialization.

            if (data.IsNull)
            {
                return null;
            }

            // Construct the object

            object? result = ConstructDeserializedClass(serializableClassInfo, data, document);

            // Fll all deserialized properties

            foreach (var prop in data.PropertyValues)
                prop.Property.Property.SetValue(result, prop.Value);

            // Finally return the deserialized object

            return result;
        }

        private object? DeserializeCustomSerializableClass(Type type, XmlElement element, ClassCustomSerializationInfo customSerializableClassInfo, XmlDocument document)
        {
            // Instantiate object

            var result = Activator.CreateInstance(type)!;

            // Cast to IXmlSerializable and run custom deserialization

            IXmlSerializable serializable = (IXmlSerializable)result;
            serializable.Read(element);
            return result;
        }

        private object? DeserializeType(Type type, XmlElement element, XmlDocument document)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(element);

            // Get class info about this type

            var classInfo = classSerializationInfoProvider.GetClassInfo(type);

            switch (classInfo)
            {
                case ClassCustomSerializationInfo customSerializableClassInfo:
                    {
                        return DeserializeCustomSerializableClass(type, element, customSerializableClassInfo, document);
                    }
                case ClassSerializationInfo serializableClassInfo:
                    {
                        return DeserializeClass(type, element, serializableClassInfo, document);
                    }
                default:
                    throw new InvalidOperationException("Unsupported class info!");
            }
        }

        private object? DeserializeExpectedTypes(Type baseType, Dictionary<string, Type> expectedDescendingTypes, XmlElement element, XmlDocument document)
        {
            ArgumentNullException.ThrowIfNull(baseType);
            ArgumentNullException.ThrowIfNull(expectedDescendingTypes);
            ArgumentNullException.ThrowIfNull(element);

            // Basic parameter checks

            if (!baseType.IsClass)
                throw new InvalidOperationException("Base type must be a class!");
            if (expectedDescendingTypes.Count < 1)
                throw new InvalidOperationException($"{nameof(expectedDescendingTypes)} must contain at least one type!");

            // Check if expected descending types are indeed descending from
            // the base one

            foreach (var kvp in expectedDescendingTypes)
                if (!kvp.Value.IsAssignableTo(baseType))
                    throw new InvalidOperationException($"Type {kvp.Value.Name} is not assignable to {baseType.Name}!");

            // Find matching type

            if (!expectedDescendingTypes.TryGetValue(element.Name, out Type? type) || type == null)
                throw new XmlSerializationException($"Node name {element.Name} does not match any of given types!");

            // Deserialize from XML

            object? result = DeserializeType(type, element, document);

            return result;
        }

        private object? DeserializeExpectedType(Type expectedType, string expectedName, XmlElement element, XmlDocument document) => 
            DeserializeExpectedTypes(expectedType,
                new Dictionary<string, Type> { { expectedName, expectedType } },
                element,
                document);

        private object? Deserialize(Type type, XmlDocument document)
        {
            if (document.ChildNodes.OfType<XmlElement>().Count() == 0)
                throw new XmlSerializationException("Empty document, cannot deserialize");

            var rootNode = document.ChildNodes.OfType<XmlElement>().First();
            return DeserializeType(type, rootNode, document);
        }

        // IXmlSerializationProvider implementation ---------------------------

        void IXmlSerializationProvider.SerializePropertyValue(Type modelType, XmlElement propertyElement, object? value, ITypeMappingProperty property, XmlDocument document) =>
            SerializePropertyValue(modelType, propertyElement, value, property, document);

        object? IXmlSerializationProvider.DeserializePropertyValue(Type modelType, XmlElement itemElement, ITypeMappingProperty property, XmlDocument document) =>
            DeserializePropertyValue(modelType, itemElement, property, document);

        // Public methods -----------------------------------------------------

        public XmlSerializer(XmlSerializerConfig? config = null)
        {            
            this.config = config ?? new XmlSerializerConfig();

            if (this.config.UseSharedTypeCache)
                classSerializationInfoProvider = new ClassInfoProvider(sharedTypeCache, sharedTypeCacheLock);
            else
                classSerializationInfoProvider = new ClassInfoProvider(new(), new());

            converterProvider = new ConverterProvider();
            collectionSerializerProvider = new CollectionSerializerProvider();
            mapSerializerProvider = new MapSerializerProvider();
        }

        public void Serialize<T>(T? model, Stream s)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(s);

            var document = new XmlDocument();
            Serialize(model, typeof(T), document);

            document.Save(s);
        }

        public T? Deserialize<T>(Stream s)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(s);

            var document = new XmlDocument();
            document.Load(s);

            var rootNode = document.ChildNodes[0];

            T? result = (T?)Deserialize(typeof(T), document);
            return result;
        }
    }
}
