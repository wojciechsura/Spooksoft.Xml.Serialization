using Spooksoft.Xml.Serialization.Conversion;
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

        private class DeserializedClassContents
        {

        }

        // Private fields -----------------------------------------------------

        private static readonly Dictionary<Type, BaseClassInfo> sharedTypeCache = new();
        private static readonly object sharedTypeCacheLock = new();

        private readonly Dictionary<Type, BaseClassInfo> typeCache;
        private readonly object typeCacheLock;
        private XmlSerializerConfig config;

        // Private methods ----------------------------------------------------

        private BaseClassInfo EnsureClassInfo(Type type)
        {
            BaseClassInfo? classInfo;

            lock(typeCacheLock)
            {
                if (!typeCache.TryGetValue(type, out classInfo))
                {
                    classInfo = ClassInfoBuilder.BuildClassInfo(type);
                    typeCache[type] = classInfo;
                }
            }

            return classInfo!;            
        }

        private IConverter? GetConverter(Type type)
        {
            if (type.IsEnum)
                return DefaultConverters.GetEnumConverter(type);

            if (DefaultConverters.Converters.TryGetValue(type, out IConverter? result))
                return result;

            return null;
        }

        private void SerializePropertyToAttribute(object? model, Type modelType, BasePropertyInfo prop, XmlElement element)
        {
            // Get property type
            var propertyType = prop.Property.PropertyType;

            // Find converter
            var converter = GetConverter(propertyType) ??
                throw new XmlSerializationException($"Cannot find converter for type {propertyType.Name} to serialize property {prop.Property.Name} of class {modelType.Name}");

            // Get value of the property
            object? value = prop.Property.GetValue(model) ?? 
                throw new XmlSerializationException($"Cannot store null value as text (e.g. in attribute). Property {prop.Property.Name} of class {modelType.Name}");

            // Serialize
            var attribute = element.OwnerDocument.CreateAttribute(prop.XmlName);
            attribute.Value = converter.Serialize(value);
            element.Attributes.Append(attribute);
        }

        private void SerializePropertyToElement(object? model, Type modelType, BasePropertyInfo prop, XmlElement element)
        {
            // Get property type
            var propertyType = prop.Property.PropertyType;

            // Get property value
            var value = prop.Property.GetValue(model);

            // Find converter
            var converter = GetConverter(propertyType);
            if (converter != null)
            {
                // If there is a converter for this type, serialize as string

                if (value == null)
                    throw new XmlSerializationException($"Cannot store null value as text (e.g. in attribute). Property {prop.Property.Name} of class {modelType.Name}");

                var subElement = element.OwnerDocument.CreateElement(prop.XmlName);
                subElement.InnerText = converter.Serialize(value);
                element.AppendChild(subElement);

                return;
            }

            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
            {
                
            }

            if (propertyType.IsClass)
            {
                // If it is a class, serialize recursively

                var propertyElement = element.OwnerDocument.CreateElement(prop.XmlName);
                var subElement = SerializeObjectToElement(value, propertyType, element.OwnerDocument);
                propertyElement.AppendChild(subElement);
                element.AppendChild(propertyElement);

                return;
            }

            throw new XmlSerializationException($"Cannot serialize property {prop.Property.Name} of class {modelType.Name}. No suitable serialization method for type {propertyType}");
        }

        private XmlElement SerializeObjectToElement(object? model, Type modelType, XmlDocument document)
        {
            ArgumentNullException.ThrowIfNull(modelType);
            ArgumentNullException.ThrowIfNull(document);

            var classInfo = EnsureClassInfo(modelType);

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
                                SerializePropertyToAttribute(model, modelType, prop, result);

                            foreach (var prop in serializableClassInfo.Properties
                                .Where(p => p.XmlPlacement == Types.XmlPlacement.Element))
                                SerializePropertyToElement(model, modelType, prop, result);

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

        private object? DeserializeClass(Type type, XmlElement element, SerializableClassInfo serializableClassInfo)
        {
            // Collect information from attributes and sub-elements

            string? nodeContents = null;
            List<PropertyValue> propertyValues = new();
            List<ConstructorParameterValue?> constructorParameterValues = new();

            foreach (XmlAttribute attribute in element.Attributes)
            {
                // Find property stored in an attribute

                var propInfo = serializableClassInfo.Properties
                    .FirstOrDefault(pi => pi.XmlName == attribute.Name && pi.XmlPlacement == Types.XmlPlacement.Attribute);

                if (propInfo == null)
                {
                    if (config.ErrorOnNotRecognizedProperties)
                        throw new XmlSerializationException($"Not recognized attribute {attribute.Name} for type {type.Name}");
                    else
                        continue;
                }

                // Get converter for found property type

                var converter = GetConverter(propInfo.Property.PropertyType) ??
                    throw new XmlSerializationException($"No converter found for type {propInfo.Property.PropertyType.Name} to deserialize property {propInfo.Property.Name} of class {type.Name}");

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
                    while (constructorParameterValues.Count <= propInfo.ConstructorParameterIndex.Value)
                        constructorParameterValues.Add(null);

                    constructorParameterValues[propInfo.ConstructorParameterIndex.Value] = new ConstructorParameterValue(propInfo, value);
                }
                else
                {
                    propertyValues.Add(new PropertyValue(propInfo, value));
                }
            }

            foreach (var subElement in element.ChildNodes.OfType<XmlElement>())
            {
                // Find and read property stored in an element

                var propInfo = serializableClassInfo.Properties.FirstOrDefault(pi => pi.XmlName == subElement.Name && pi.XmlPlacement == Types.XmlPlacement.Element);
                if (propInfo == null)
                {
                    if (config.ErrorOnNotRecognizedProperties)
                        throw new XmlSerializationException($"Not recognized element {subElement.Name} for type {type.Name}");
                    else
                    {
                        // Skip contents of the current property                                    
                        continue;
                    }
                }

                // Try to get a converter for property type

                var converter = GetConverter(propInfo.Property.PropertyType);

                object? value = null;

                if (converter != null)
                {
                    // The node is expected to contain text only
                    string? contents = subElement.InnerText;
                    if (contents == null)
                        contents = string.Empty;

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
                        throw new XmlSerializationException($"Expected a single sub-element for node {subElement.Name} representing property {propInfo.Property.Name} of class {type.Name}");

                    var child = children[0];

                    value = DeserializeExpectedType(propertyType, child);
                }

                // Store deserialized value, depending on kind of property

                if (propInfo.ConstructorParameterIndex != null)
                {
                    while (constructorParameterValues.Count <= propInfo.ConstructorParameterIndex.Value)
                        constructorParameterValues.Add(null);

                    constructorParameterValues[propInfo.ConstructorParameterIndex.Value] = new ConstructorParameterValue(propInfo, value);
                }
                else
                {
                    propertyValues.Add(new PropertyValue(propInfo, value));
                }
            }

            if (element.ChildNodes.OfType<XmlElement>().Count() == 0 &&
                element.Attributes.Count == 0)
                nodeContents = element.InnerText;

            // Collected all information about the deserialized object
            // Time to instantiate it and actually fill with deserialized
            // data.

            if (nodeContents != null)
            {
                // The only valid case is text "null" in which case
                // we simply return null

                if (nodeContents == "null")
                {
                    if (propertyValues.Count > 0 || constructorParameterValues.Count > 0)
                        throw new XmlSerializationException($"Error while deserializing type {type.Name}: element can only contain text \"null\" if there are no attributes or elements present!");

                    return null;
                }
                else
                {
                    throw new XmlSerializationException($"The only valid text content of an element representing class instance is \"null\", found \"{nodeContents}\" instead.");
                }
            }

            // Construct the object

            object? result = null;

            switch (serializableClassInfo.ConstructionInfo)
            {
                case ParameterlessCtorConstructionInfo:
                    {
                        result = Activator.CreateInstance(type)!;

                        break;
                    }
                case ParameteredCtorConstructionInfo paramCtor:
                    {
                        // Ensure that there are enough values
                        if (constructorParameterValues.Count > paramCtor.ConstructorParameters.Count)
                            throw new InvalidOperationException("Algorithm error: more constructor parameters deserialized than constructor actually have!");

                        while (constructorParameterValues.Count < paramCtor.ConstructorParameters.Count)
                            constructorParameterValues.Add(null);

                        // Ensure that there are no missing values
                        // Fill those with default values if user wants that
                        for (int i = 0; i < paramCtor.ConstructorParameters.Count; i++)
                        {
                            if (constructorParameterValues[i] == null)
                            {
                                if (config.ReplaceMissingCtorParamsWithDefaultValues)
                                {
                                    var propertyInfo = serializableClassInfo.Properties
                                        .Where(p => p.ConstructorParameterIndex != null)
                                        .Single(p => p.Property == paramCtor.ConstructorParameters[i].MatchingProperty);

                                    var paramType = paramCtor.ConstructorParameters[i].MatchingProperty.PropertyType;
                                    object? defaultValue = paramType.IsValueType ? Activator.CreateInstance(paramType) : null;

                                    var replacementValue = new ConstructorParameterValue(propertyInfo, defaultValue);
                                    constructorParameterValues[i] = replacementValue;
                                }
                                else
                                    throw new XmlSerializationException($"Failed to find value for parameter {i} of the constructor of type {type.Name}");
                            }
                        }

                        // Now instantiate class

                        result = Activator.CreateInstance(type, constructorParameterValues.Select(pv => pv!.Value).ToArray())!;

                        break;
                    }
                default:
                    throw new InvalidOperationException("Unsupported construction info!");
            }

            // Now fill all deserialized properties

            foreach (var prop in propertyValues)
            {
                prop.Property.Property.SetValue(result, prop.Value);
            }

            // Finally return the deserialized object

            return result;
        }

        private object? DeserializeCustomSerializableClass(Type type, XmlElement element, CustomSerializableClassInfo customSerializableClassInfo)
        {
            // Instantiate object

            var result = Activator.CreateInstance(type)!;
            IXmlSerializable serializable = (IXmlSerializable)result;
            serializable.Read(element);
            return result;
        }

        private object? DeserializeType(Type type, XmlElement element)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(element);

            // Get class info about this type

            var classInfo = EnsureClassInfo(type);

            switch (classInfo)
            {
                case CustomSerializableClassInfo customSerializableClassInfo:
                    {
                        return DeserializeCustomSerializableClass(type, element, customSerializableClassInfo);
                    }
                case SerializableClassInfo serializableClassInfo:
                    {
                        return DeserializeClass(type, element, serializableClassInfo);
                    }
                default:
                    throw new InvalidOperationException("Unsupported class info!");
            }
        }

        private object? DeserializeExpectedTypes(Type baseType, List<Type> expectedDescendingTypes, XmlElement element)
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
                .Select(t => EnsureClassInfo(t))
                .ToList();

            // Start processing XML

            int i = classInfos.Count - 1;
            while (i >= 0 && classInfos[i].XmlRoot != element.Name)
                i--;

            if (i < 0)
                throw new XmlSerializationException($"Node name {element.Name} does not match any of given types!");

            object? result = DeserializeType(classInfos[i].Type, element);

            // It is expected that DeserializeType will stop at the closing tag

            return result;
        }

        private object? DeserializeExpectedType(Type expectedType, XmlElement element)            
        {
            ArgumentNullException.ThrowIfNull(expectedType);
            ArgumentNullException.ThrowIfNull(element);

            if (!expectedType.IsClass)
                throw new InvalidOperationException("Type passed to DeserializeExpectedType must be a class!");

            return DeserializeExpectedTypes(expectedType, new List<Type> { expectedType }, element);
        }

        private object? Deserialize(Type type, XmlDocument document)
        {
            if (document.ChildNodes.Count == 0)
                throw new XmlSerializationException("Empty document, cannot deserialize");

            var rootNode = (XmlElement)document.ChildNodes[0]!;
            return DeserializeType(type, rootNode);
        }

        // Public methods -----------------------------------------------------

        public XmlSerializer(XmlSerializerConfig? config = null)
        {            
            this.config = config ?? new XmlSerializerConfig();

            if (this.config.UseSharedTypeCache)
            {
                typeCache = sharedTypeCache;
                typeCacheLock = sharedTypeCacheLock;
            }
            else
            {
                typeCache = new();
                typeCacheLock = new();
            }
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
