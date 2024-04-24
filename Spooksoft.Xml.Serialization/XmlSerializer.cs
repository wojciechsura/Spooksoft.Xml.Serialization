using Spooksoft.Xml.Serialization.Exceptions;
using Spooksoft.Xml.Serialization.Infrastructure;
using Spooksoft.Xml.Serialization.Models;
using Spooksoft.Xml.Serialization.Models.Construction;
using Spooksoft.Xml.Serialization.Models.Properties;
using Spooksoft.Xml.Serialization.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Spooksoft.Xml.Serialization
{
    public class XmlSerializer
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
            public string? NodeContents { get; set; } = null;
            public List<PropertyValue> PropertyValues { get; } = new();
            public List<ConstructorParameterValue?> ConstructorParameterValues { get; } = new();
        }

        // Private fields -----------------------------------------------------

        private static readonly Dictionary<Type, BaseClassInfo> sharedTypeCache = new();
        private static readonly object sharedTypeCacheLock = new();

        private readonly IClassInfoProvider classInfoProvider;
        private readonly IConverterProvider converterProvider;

        private XmlSerializerConfig config;

        // Private methods ----------------------------------------------------
       
        private void SerializePropertyToAttribute(object model, 
            BasePropertyInfo prop, 
            XmlElement classElement, 
            XmlDocument document)
        {
            // Get property type
            var propertyType = prop.Property.PropertyType;

            // Find converter
            var converter = converterProvider.GetConverter(propertyType) ??
                throw new XmlSerializationException($"Cannot find converter for type {propertyType.Name} to serialize property {prop.Property.Name} of class {model.GetType().Name}");

            // Get value of the property
            object? value = prop.Property.GetValue(model) ?? 
                throw new XmlSerializationException($"Cannot store null value as text (e.g. in attribute). Property {prop.Property.Name} of class {model.GetType().Name}");

            // Serialize
            var attribute = document.CreateAttribute(prop.XmlName);
            attribute.Value = converter.Serialize(value);
            classElement.Attributes.Append(attribute);
        }

        private void SerializePropertyToElement(object model,
            BasePropertyInfo prop, 
            XmlElement classElement,
            XmlDocument document)
        {
            // Get property type
            var propertyType = prop.Property.PropertyType;

            // Get property value
            var value = prop.Property.GetValue(model);

            // Find converter
            var converter = converterProvider.GetConverter(propertyType);
            if (converter != null)
            {
                // If there is a converter for this type, serialize as string

                if (value == null)
                    throw new XmlSerializationException($"Cannot store null value as text (e.g. in attribute). Property {prop.Property.Name} of class {model.GetType().Name}");

                var element = document.CreateElement(prop.XmlName);
                element.InnerText = converter.Serialize(value);
                classElement.AppendChild(element);

                return;
            }

            if (propertyType.IsClass)
            {
                // If it is a class, serialize recursively

                var propertyElement = document.CreateElement(prop.XmlName);
                var subElement = SerializeObjectToElement(value, propertyType, classElement.OwnerDocument);
                propertyElement.AppendChild(subElement);
                classElement.AppendChild(propertyElement);

                return;
            }

            throw new XmlSerializationException($"Cannot serialize property {prop.Property.Name} of class {model.GetType().Name}. No suitable serialization method for type {propertyType}");
        }

        private XmlElement SerializeObjectToElement(object? model, Type modelType, XmlDocument document)
        {
            ArgumentNullException.ThrowIfNull(modelType);
            ArgumentNullException.ThrowIfNull(document);

            var classInfo = classInfoProvider.GetClassInfo(modelType);

            var result = document.CreateElement(classInfo.XmlRoot);

            if (model == null)
            {
                result.InnerText = "null";
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

                // Get converter for found property type

                var converter = converterProvider.GetConverter(propInfo.Property.PropertyType) ??
                    throw new XmlSerializationException($"No converter found for type {propInfo.Property.PropertyType.Name} to deserialize property {propInfo.Property.Name} of class {data.SerializableClassInfo.Type.Name}");

                // Deserialize value of the property

                object? value = null;
                try
                {
                    value = converter.Deserialize(attribute.Value);
                }
                catch (Exception e)
                {
                    if (config.ErrorOnFailedAttributeDeserialization)
                        throw new XmlSerializationException($"Failed to deserialize attribute value {attribute.Value} to {propInfo.Property.PropertyType.Name}", e);
                    else
                        continue;
                }

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

                // Try to get a converter for property type

                var converter = converterProvider.GetConverter(propInfo.Property.PropertyType);

                object? value = null;

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

                    value = DeserializeExpectedType(propertyType, child, document);
                }

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

        private void CollectXmlInnerTextDeserializationData(ClassDeserializationData data, XmlElement element, XmlDocument document)
        {
            if (!element.ChildNodes.OfType<XmlElement>().Any() &&
                            element.Attributes.Count == 0)
                data.NodeContents = element.InnerText;
        }

        private object? DeserializeClass(Type type, SerializableClassInfo serializableClassInfo, XmlElement element, XmlDocument document)
        {
            // Collect information from attributes and sub-elements

            ClassDeserializationData deserializationContext = new(serializableClassInfo);
            CollectXmlAttributeDeserializationData(deserializationContext, element, document);
            CollectXmlElementDeserializationData(deserializationContext, element, document);
            CollectXmlInnerTextDeserializationData(deserializationContext, element, document);

            // Collected all information about the deserialized object
            // Time to instantiate it and actually fill with deserialized
            // data.

            if (deserializationContext.NodeContents != null)
            {
                // The only valid case is text "null" in which case
                // we simply return null

                if (deserializationContext.NodeContents == "null")
                {
                    if (deserializationContext.PropertyValues.Count > 0 || deserializationContext.ConstructorParameterValues.Count > 0)
                        throw new XmlSerializationException($"Error while deserializing type {type.Name}: element can only contain text \"null\" if there are no attributes or elements present!");

                    return null;
                }
                else
                {
                    throw new XmlSerializationException($"The only valid text content of an element representing class instance is \"null\", found \"{deserializationContext.NodeContents}\" instead.");
                }
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

        private object? DeserializeExpectedTypes(Type baseType, List<Type> expectedDescendingTypes, XmlElement element, XmlDocument document)
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

            foreach (var type in expectedDescendingTypes)
                if (!type.IsAssignableTo(baseType))
                    throw new InvalidOperationException($"Type {type.Name} is not assignable to {baseType.Name}!");

            // Collect class infos for all types

            var classInfos = expectedDescendingTypes
                .Select(t => classInfoProvider.GetClassInfo(t))
                .ToList();

            // Start processing XML

            int i = classInfos.Count - 1;
            while (i >= 0 && classInfos[i].XmlRoot != element.Name)
                i--;

            if (i < 0)
                throw new XmlSerializationException($"Node name {element.Name} does not match any of given types!");

            object? result = DeserializeType(classInfos[i].Type, element, document);

            // It is expected that DeserializeType will stop at the closing tag

            return result;
        }

        private object? DeserializeExpectedType(Type expectedType, XmlElement element, XmlDocument document)            
        {
            ArgumentNullException.ThrowIfNull(expectedType);
            ArgumentNullException.ThrowIfNull(element);

            if (!expectedType.IsClass)
                throw new InvalidOperationException("Type passed to DeserializeExpectedType must be a class!");

            return DeserializeExpectedTypes(expectedType, new List<Type> { expectedType }, element, document);
        }

        private object? Deserialize(Type type, XmlDocument document)
        {
            if (document.ChildNodes.Count == 0)
                throw new XmlSerializationException("Empty document, cannot deserialize");

            var rootNode = (XmlElement)document.ChildNodes[0]!;
            return DeserializeType(type, rootNode, document);
        }

        // Public methods -----------------------------------------------------

        public XmlSerializer(XmlSerializerConfig? config = null)
        {            
            this.config = config ?? new XmlSerializerConfig();

            if (this.config.UseSharedTypeCache)
                classInfoProvider = new ClassInfoProvider(sharedTypeCache, sharedTypeCacheLock);
            else
                classInfoProvider = new ClassInfoProvider(new(), new());

            converterProvider = new ConverterProvider();
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
