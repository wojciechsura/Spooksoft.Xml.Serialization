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
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Spooksoft.Xml.Serialization
{
    public class XmlSerializer : IXmlSerializationProvider
    {
        // Private types ------------------------------------------------------

        private class PropertyValue
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

        private class ConstructorParameterValue
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

        private class ClassDeserializationData
        {
            public ClassDeserializationData(SerializableClassInfo serializableClassInfo)
            {
                SerializableClassInfo = serializableClassInfo;
            }

            public SerializableClassInfo SerializableClassInfo { get; }
            public bool IsNull { get; set; } = false;
            public List<PropertyValue> PropertyValues { get; } = new();
            public List<ConstructorParameterValue?> ConstructorParameterValues { get; } = new();
        }

        // Private fields -----------------------------------------------------

        private static readonly Dictionary<Type, BaseClassInfo> sharedTypeCache = new();
        private static readonly object sharedTypeCacheLock = new();

        private readonly IClassInfoProvider classInfoProvider;
        private readonly IConverterProvider converterProvider;
        private readonly ICollectionSerializerProvider collectionSerializerProvider;

        private XmlSerializerConfig config;

        // Private methods ----------------------------------------------------
       
        private void SerializePropertyToAttribute(object model, 
            BasePropertyInfo prop, 
            XmlElement classElement, 
            XmlDocument document)
        {
            if (prop is SimplePropertyInfo)
            {
                // Get property type
                var propertyType = prop.Property.PropertyType;

                // Find converter
                var converter = converterProvider.GetConverter(propertyType) ??
                    throw new XmlSerializationException($"Cannot find converter for type {propertyType.Name} to serialize property {prop.Property.Name} of class {model.GetType().Name}");

                // Get value of the property
                object? value = prop.Property.GetValue(model);

                // Serialize
                var attribute = document.CreateAttribute(prop.XmlName);
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
                // Get property type
                var propertyType = simpleProp.Property.PropertyType;

                // Get property value
                var value = simpleProp.Property.GetValue(model);

                // Find converter
                var converter = converterProvider.GetConverter(propertyType);
                if (converter != null)
                {
                    // If there is a converter for this type, serialize as string                    
                    var element = document.CreateElement(simpleProp.XmlName);
                    element.InnerText = converter.Serialize(value);
                    classElement.AppendChild(element);
                }
                else if (propertyType.IsClass)
                {
                    // If it is a class, serialize recursively

                    var propertyElement = document.CreateElement(simpleProp.XmlName);
                    var subElement = SerializeObjectToElement(value, propertyType, classElement.OwnerDocument);
                    propertyElement.AppendChild(subElement);
                    classElement.AppendChild(propertyElement);
                }
                else throw new XmlSerializationException($"Cannot serialize property {simpleProp.Property.Name} of class {model.GetType().Name}. No suitable serialization method for type {propertyType.Name}");
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

                var propertyElement = document.CreateElement(collectionProp.XmlName);
                serializer.Serialize(value, model.GetType(), collectionProp, propertyElement, document, this);
                classElement.AppendChild(propertyElement);
            }
            else
                throw new InvalidOperationException("Unsupported property info!");
        }

        private XmlElement SerializeObjectToElement(object? model, Type modelType, XmlDocument document)
        {
            ArgumentNullException.ThrowIfNull(modelType);
            ArgumentNullException.ThrowIfNull(document);

            var classInfo = classInfoProvider.GetClassInfo(modelType);

            return SerializeObjectToElement(model, modelType, classInfo.XmlRoot, document);
        }

        private XmlElement SerializeObjectToElement(object? model, Type modelType, string elementName, XmlDocument document)
        {
            ArgumentNullException.ThrowIfNull(modelType);
            ArgumentNullException.ThrowIfNull(document);

            var classInfo = classInfoProvider.GetClassInfo(modelType);

            var result = document.CreateElement(elementName);

            if (model == null)
            {
                result.SetAttribute(Constants.NIL_ATTRIBUTE, Constants.CONTROL_NAMESPACE_URI, "true");
            }
            else
            {                
                switch (classInfo)
                {
                    case CustomSerializableClassInfo:
                        {
                            // User serializes contents of this class on his own

                            var serializable = (IXmlSerializable)model;
                            serializable.Write(result);
                            break;
                        }
                    case SerializableClassInfo serializableClassInfo:
                        {
                            // Class is serialized automatically

                            foreach (var prop in serializableClassInfo.Properties
                                .Where(p => p.XmlPlacement == Types.XmlPlacement.Attribute))
                                SerializePropertyToAttribute(model, prop, result, document);

                            foreach (var prop in serializableClassInfo.Properties
                                .Where(p => p.XmlPlacement == Types.XmlPlacement.Element))
                                SerializePropertyToElement(model, prop, result, document);

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

            var element = SerializeObjectToElement(model, modelType, document);
            element.SetAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
            
            document.AppendChild(element);
        }

        private object ConstructDeserializedClass(SerializableClassInfo serializableClassInfo, ClassDeserializationData deserializationContext, XmlDocument document)
        {
            object? result = null;

            switch (serializableClassInfo.ConstructionInfo)
            {
                case ParameterlessCtorConstructionInfo:
                    {
                        result = Activator.CreateInstance(serializableClassInfo.Type)!;

                        break;
                    }
                case ParameteredCtorConstructionInfo paramCtor:
                    {
                        // Ensure that there are enough values
                        if (deserializationContext.ConstructorParameterValues.Count > paramCtor.ConstructorParameters.Count)
                            throw new InvalidOperationException("Algorithm error: more constructor parameters deserialized than constructor actually have!");

                        while (deserializationContext.ConstructorParameterValues.Count < paramCtor.ConstructorParameters.Count)
                            deserializationContext.ConstructorParameterValues.Add(null);

                        // Ensure that there are no missing values
                        // Fill those with default values if user wants that
                        for (int i = 0; i < paramCtor.ConstructorParameters.Count; i++)
                        {
                            if (deserializationContext.ConstructorParameterValues[i] == null)
                            {
                                if (config.ReplaceMissingCtorParamsWithDefaultValues)
                                {
                                    var propertyInfo = serializableClassInfo.Properties
                                        .Where(p => p.ConstructorParameterIndex != null)
                                        .Single(p => p.Property == paramCtor.ConstructorParameters[i].MatchingProperty);

                                    var paramType = paramCtor.ConstructorParameters[i].MatchingProperty.PropertyType;
                                    object? defaultValue = paramType.IsValueType ? Activator.CreateInstance(paramType) : null;

                                    var replacementValue = new ConstructorParameterValue(propertyInfo, defaultValue);
                                    deserializationContext.ConstructorParameterValues[i] = replacementValue;
                                }
                                else
                                    throw new XmlSerializationException($"Failed to find value for parameter {i} of the constructor of type {serializableClassInfo.Type.Name}");
                            }
                        }

                        // Now instantiate class

                        result = Activator.CreateInstance(serializableClassInfo.Type, deserializationContext.ConstructorParameterValues.Select(pv => pv!.Value).ToArray())!;

                        break;
                    }
                default:
                    throw new InvalidOperationException("Unsupported construction info!");
            }

            return result;
        }

        private void CollectXmlAttributeDeserializationData(ClassDeserializationData data, XmlElement element, XmlDocument document)
        {
            foreach (XmlAttribute attribute in element.Attributes)
            {
                // Skip namespace definition attributes

                if (attribute.Name == "xmlns" || attribute.Name.StartsWith("xmlns:"))
                    continue;

                // Skip namespaced attributes - they are dealt with differently

                if (!string.IsNullOrEmpty(attribute.NamespaceURI))
                    continue;

                // Find property stored in an attribute

                var propInfo = data.SerializableClassInfo.Properties
                    .FirstOrDefault(pi => pi.XmlName == attribute.Name && pi.XmlPlacement == Types.XmlPlacement.Attribute);

                if (propInfo == null)
                {
                    if (config.ErrorOnNotRecognizedProperties)
                        throw new XmlSerializationException($"Not recognized attribute {attribute.Name} for type {data.SerializableClassInfo.Type.Name}");
                    else
                        continue;
                }

                if (propInfo is not SimplePropertyInfo simplePropInfo)
                    throw new XmlSerializationException($"Property {propInfo.Property.Name} of class {data.SerializableClassInfo.Type.Name} is defined as an {nameof(XmlArrayAttribute)}, which means it can be placed only in an element.");

                // Get converter for found property type

                var converter = converterProvider.GetConverter(simplePropInfo.Property.PropertyType) ??
                    throw new XmlSerializationException($"No converter found for type {simplePropInfo.Property.PropertyType.Name} to deserialize property {simplePropInfo.Property.Name} of class {data.SerializableClassInfo.Type.Name}");

                // Deserialize value of the property

                object? value = null;
                try
                {
                    value = converter.Deserialize(attribute.Value);
                }
                catch (Exception e)
                {
                    if (config.ErrorOnFailedAttributeDeserialization)
                        throw new XmlSerializationException($"Failed to deserialize attribute value {attribute.Value} to {simplePropInfo.Property.PropertyType.Name}", e);
                    else
                        continue;
                }

                // Store deserialized value, depending on kind of property

                if (simplePropInfo.ConstructorParameterIndex != null)
                {
                    while (data.ConstructorParameterValues.Count <= simplePropInfo.ConstructorParameterIndex.Value)
                        data.ConstructorParameterValues.Add(null);

                    data.ConstructorParameterValues[simplePropInfo.ConstructorParameterIndex.Value] = new ConstructorParameterValue(simplePropInfo, value);
                }
                else
                {
                    data.PropertyValues.Add(new PropertyValue(simplePropInfo, value));
                }
            }
        }

        private void CollectXmlElementDeserializationData(ClassDeserializationData data, XmlElement element, XmlDocument document)
        {
            foreach (var subElement in element.ChildNodes.OfType<XmlElement>())
            {
                // Find and read property stored in an element

                var propInfo = data.SerializableClassInfo.Properties.FirstOrDefault(pi => pi.XmlName == subElement.Name && pi.XmlPlacement == Types.XmlPlacement.Element);
                if (propInfo == null)
                {
                    if (config.ErrorOnNotRecognizedProperties)
                        throw new XmlSerializationException($"Not recognized element {subElement.Name} for type {data.SerializableClassInfo.Type.Name}");
                    else
                    {
                        // Skip contents of the current property                                    
                        continue;
                    }
                }

                object? value = null;

                if (propInfo is SimplePropertyInfo simplePropInfo)
                {
                    // Try to get a converter for property type

                    var converter = converterProvider.GetConverter(propInfo.Property.PropertyType);

                    if (converter != null)
                    {
                        // The node is expected to contain text only
                        string? contents = subElement.InnerText ?? string.Empty;

                        // Try to deserialize

                        try
                        {
                            value = converter.Deserialize(contents);
                        }
                        catch (Exception e)
                        {
                            if (config.ErrorOnNotRecognizedProperties)
                                throw new XmlSerializationException($"Failed to deserialize element value {subElement.Value} to {propInfo.Property.PropertyType.Name}", e);
                            else
                                continue;
                        }
                    }
                    else
                    {
                        var propertyType = propInfo.Property.PropertyType;

                        var children = subElement.ChildNodes.OfType<XmlElement>().ToArray();
                        if (children.Length != 1)
                            throw new XmlSerializationException($"Expected a single sub-element for node {subElement.Name} representing property {propInfo.Property.Name} of class {data.SerializableClassInfo.Type.Name}");

                        var child = children[0];

                        value = DeserializeExpectedType(propertyType, child.Name, child, document);
                    }
                }
                else if (propInfo is CollectionPropertyInfo collectionProp)
                {
                    // Try to get a serializer for collection

                    var serializer = collectionSerializerProvider.GetCollectionSerializer(collectionProp.Property.PropertyType) ?? 
                        throw new XmlSerializationException($"Cannot deserialize property {collectionProp.Property.Name} of class {data.SerializableClassInfo.Type.Name}. No suitable collection serializer found for type {collectionProp.Property.PropertyType.Name}");

                    value = serializer.Deserialize(data.SerializableClassInfo.Type, collectionProp, subElement, document, this);
                }
                else
                    throw new InvalidOperationException("Unsupported property info!");

                // Store deserialized value, depending on kind of property

                if (propInfo.ConstructorParameterIndex != null)
                {
                    while (data.ConstructorParameterValues.Count <= propInfo.ConstructorParameterIndex.Value)
                        data.ConstructorParameterValues.Add(null);

                    data.ConstructorParameterValues[propInfo.ConstructorParameterIndex.Value] = new ConstructorParameterValue(propInfo, value);
                }
                else
                {
                    data.PropertyValues.Add(new PropertyValue(propInfo, value));
                }
            }
        }

        private void CollectNullDeserializationData(ClassDeserializationData data, XmlElement element, XmlDocument document)
        {
            var attribute = element.Attributes.OfType<XmlAttribute>().FirstOrDefault(attr => attr.NamespaceURI == Constants.CONTROL_NAMESPACE_URI && attr.LocalName == Constants.NIL_ATTRIBUTE);
            if (attribute != null && attribute.Value.ToLower() == "true")
            {
                data.IsNull = true;
            }
        }

        private object? DeserializeClass(Type type, SerializableClassInfo serializableClassInfo, XmlElement element, XmlDocument document)
        {
            // Collect information from attributes and sub-elements

            ClassDeserializationData deserializationContext = new(serializableClassInfo);
            CollectXmlAttributeDeserializationData(deserializationContext, element, document);
            CollectXmlElementDeserializationData(deserializationContext, element, document);
            CollectNullDeserializationData(deserializationContext, element, document);

            // Collected all information about the deserialized object
            // Time to instantiate it and actually fill with deserialized
            // data.

            if (deserializationContext.IsNull)
            {
                return null;
            }

            // Construct the object

            object? result = ConstructDeserializedClass(serializableClassInfo, deserializationContext, document);

            // Now fill all deserialized properties

            foreach (var prop in deserializationContext.PropertyValues)
            {
                prop.Property.Property.SetValue(result, prop.Value);
            }

            // Finally return the deserialized object

            return result;
        }

        private object? DeserializeCustomSerializableClass(Type type, XmlElement element, CustomSerializableClassInfo customSerializableClassInfo, XmlDocument document)
        {
            // Instantiate object

            var result = Activator.CreateInstance(type)!;
            IXmlSerializable serializable = (IXmlSerializable)result;
            serializable.Read(element);
            return result;
        }

        private object? DeserializeType(Type type, XmlElement element, XmlDocument document)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(element);

            // Get class info about this type

            var classInfo = classInfoProvider.GetClassInfo(type);

            switch (classInfo)
            {
                case CustomSerializableClassInfo customSerializableClassInfo:
                    {
                        return DeserializeCustomSerializableClass(type, element, customSerializableClassInfo, document);
                    }
                case SerializableClassInfo serializableClassInfo:
                    {
                        return DeserializeClass(type, serializableClassInfo, element, document);
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

            // Start processing XML

            if (!expectedDescendingTypes.TryGetValue(element.Name, out Type? type) || type == null)
                throw new XmlSerializationException($"Node name {element.Name} does not match any of given types!");

            object? result = DeserializeType(type, element, document);

            return result;
        }

        private object? DeserializeExpectedType(Type expectedType, string expectedName, XmlElement element, XmlDocument document)
        {
            ArgumentNullException.ThrowIfNull(expectedType);
            ArgumentNullException.ThrowIfNull(element);

            if (!expectedType.IsClass)
                throw new InvalidOperationException("Type passed to DeserializeExpectedType must be a class!");

            return DeserializeExpectedTypes(expectedType, new Dictionary<string, Type> { { expectedName, expectedType } }, element, document);
        }

        private object? Deserialize(Type type, XmlDocument document)
        {
            if (document.ChildNodes.Count == 0)
                throw new XmlSerializationException("Empty document, cannot deserialize");

            var rootNode = (XmlElement)document.ChildNodes[0]!;
            return DeserializeType(type, rootNode, document);
        }

        // IXmlSerializationProvider implementation ---------------------------

        XmlElement IXmlSerializationProvider.SerializeObjectToElement(object? model, Type modelType, string elementName, XmlDocument document)
            => SerializeObjectToElement(model, modelType, elementName, document);

        object? IXmlSerializationProvider.DeserializeExpectedTypes(Type baseType, Dictionary<string, Type> expectedDescendingTypes, XmlElement element, XmlDocument document)
            => DeserializeExpectedTypes(baseType, expectedDescendingTypes, element, document);

        object? IXmlSerializationProvider.DeserializeExpectedType(Type expectedType, string expectedName, XmlElement element, XmlDocument document)
            => DeserializeExpectedType(expectedType, expectedName, element, document);

        IConverterProvider IXmlSerializationProvider.ConverterProvider => converterProvider;

        IClassInfoProvider IXmlSerializationProvider.ClassInfoProvider => classInfoProvider;

        // Public methods -----------------------------------------------------

        public XmlSerializer(XmlSerializerConfig? config = null)
        {            
            this.config = config ?? new XmlSerializerConfig();

            if (this.config.UseSharedTypeCache)
                classInfoProvider = new ClassInfoProvider(sharedTypeCache, sharedTypeCacheLock);
            else
                classInfoProvider = new ClassInfoProvider(new(), new());

            converterProvider = new ConverterProvider();
            collectionSerializerProvider = new CollectionSerializerProvider();
        }

        public void Serialize<T>(T model, Stream s)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(model);
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
