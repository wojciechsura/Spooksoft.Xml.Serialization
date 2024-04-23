using Spooksoft.Xml.Serialization.Conversion;
using Spooksoft.Xml.Serialization.Exceptions;
using Spooksoft.Xml.Serialization.Infrastructure;
using Spooksoft.Xml.Serialization.Models;
using Spooksoft.Xml.Serialization.Models.Construction;
using Spooksoft.Xml.Serialization.Models.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Spooksoft.Xml.Serialization
{
    public class XmlSerializer
    {
        // Private types ------------------------------------------------------

        private class PropertyValue
        {
            public PropertyValue(SimplePropertyInfo property, object? value)
            {
                Property = property;
                Value = value;
            }

            public SimplePropertyInfo Property { get; }
            public object? Value { get; }
        }

        private class ConstructorParameterValue
        {
            public ConstructorParameterValue(ConstructorParameterPropertyInfo? parameter, object? value)
            {
                Parameter = parameter;
                Value = value;
            }

            public ConstructorParameterPropertyInfo? Parameter { get; }
            public object? Value { get; }
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
                if (typeCache.TryGetValue(type, out classInfo))
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

        private void SerializePropertyToAttribute(object? model, Type modelType, XmlWriter writer, BasePropertyInfo prop)
        {
            // Get property type
            var propertyType = prop.Property.PropertyType;

            // Find converter
            var converter = GetConverter(propertyType) ??
                throw new XmlSerializationException($"Cannot find converter for type {propertyType.Name} to serialize property {prop.Property.Name} of class {modelType.Name}");

            // Get value of the property
            object? value = prop.Property.GetValue(model);

            // Serialize
            writer.WriteAttributeString(prop.XmlName, converter.Serialize(value));
        }

        private void SerializePropertyToElement(object? model, Type modelType, XmlWriter writer, BasePropertyInfo prop)
        {
            // Get property type
            var propertyType = prop.Property.PropertyType;

            // Find converter
            var converter = GetConverter(propertyType);
            if (converter != null)
            {
                // If there is a converter for this type, serialize as string

                var value = prop.Property.GetValue(model);

                writer.WriteStartElement(prop.XmlName);
                writer.WriteString(converter.Serialize(value));
                writer.WriteEndElement();

                return;
            }

            if (propertyType.IsClass)
            {
                // If it is a class, serialize recursively

                var value = prop.Property.GetValue(model);

                writer.WriteStartElement(prop.XmlName);
                Serialize(value, propertyType, writer);
                writer.WriteEndElement();

                return;
            }

            throw new XmlSerializationException($"Cannot serialize property {prop.Property.Name} of class {modelType.Name}. No suitable serialization method for type {propertyType}");
        }

        private void Serialize(object? model, Type modelType, XmlWriter writer)
        {
            ArgumentNullException.ThrowIfNull(modelType);
            ArgumentNullException.ThrowIfNull(writer);

            var classInfo = EnsureClassInfo(modelType);

            writer.WriteStartElement(classInfo.XmlRoot);

            if (model == null)
            {
                writer.WriteString("null");
            }
            else
            {                
                switch (classInfo)
                {
                    case CustomSerializableClassInfo:
                        {
                            // User serializes contents of this class on his own

                            var serializable = (IXmlSerializable)model;
                            serializable.WriteXml(writer);
                            break;
                        }
                    case SerializableClassInfo serializableClassInfo:
                        {
                            // Class is serialized automatically

                            foreach (var prop in serializableClassInfo.Properties
                                .Where(p => p.XmlPlacement == Types.XmlPlacement.Attribute))
                                SerializePropertyToAttribute(model, modelType, writer, prop);

                            foreach (var prop in serializableClassInfo.Properties
                                .Where(p => p.XmlPlacement == Types.XmlPlacement.Element))
                                SerializePropertyToElement(model, modelType, writer, prop);

                            break;
                        }
                    default:
                        throw new InvalidOperationException("Unsupported class info!");
                }
            }

            writer.WriteEndElement();
        }

        private object? DeserializeType(Type type, XmlReader reader)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(reader);

            // It is expected that opening tag for this type has
            // already been read.

            // Get class info about this type

            var classInfo = EnsureClassInfo(type);

            switch (classInfo)
            {
                case CustomSerializableClassInfo customSerializableClassInfo:
                    {
                        // Instantiate object

                        var result = Activator.CreateInstance(type)!;
                        IXmlSerializable serializable = (IXmlSerializable)result;
                        serializable.ReadXml(reader);
                        return result;
                    }
                case SerializableClassInfo serializableClassInfo:
                    {
                        // Collect information from attributes and sub-elements

                        string? nodeContents = null;
                        List<PropertyValue> propertyValues = new();
                        List<ConstructorParameterValue?> constructorParameterValues = new();

                        while (reader.Read() && reader.NodeType != XmlNodeType.EndElement)
                        {
                            if (reader.NodeType == XmlNodeType.Attribute)
                            {
                                // Find property stored in an attribute

                                var propInfo = serializableClassInfo.Properties.FirstOrDefault(pi => pi.XmlName == reader.Name && pi.XmlPlacement == Types.XmlPlacement.Attribute);
                                if (propInfo == null)
                                {
                                    if (config.ErrorOnNotRecognizedProperties)
                                        throw new XmlSerializationException($"Not recognized attribute {reader.Name} for type {type.Name}");
                                    else
                                        continue;
                                }

                                // Get converter for found property type

                                var converter = GetConverter(propInfo.Property.PropertyType);
                                if (converter == null)
                                    throw new XmlSerializationException($"No converter found for type {propInfo.Property.PropertyType.Name} to deserialize property {propInfo.Property.Name} of class {type.Name}");

                                // Deserialize value of the property

                                object? value = null;
                                try
                                {
                                    value = converter.Deserialize(reader.Value);
                                }
                                catch (Exception e)
                                {
                                    if (config.ErrorOnFailedAttributeDeserialization)
                                        throw new XmlSerializationException($"Failed to deserialize attribute value {reader.Value} to {propInfo.Property.PropertyType.Name}", e);
                                    else
                                        continue;
                                }

                                // Store deserialized value, depending on kind of property

                                switch (propInfo)
                                {
                                    case ConstructorParameterPropertyInfo constructorParamInfo:
                                        {
                                            while (constructorParameterValues.Count <= constructorParamInfo.Index)
                                                constructorParameterValues.Add(null);

                                            constructorParameterValues[constructorParamInfo.Index] = new ConstructorParameterValue(constructorParamInfo, value);
                                            break;
                                        }
                                    case SimplePropertyInfo simpleProperty:
                                        {
                                            propertyValues.Add(new PropertyValue(simpleProperty, value));
                                            break;
                                        }
                                    default:
                                        throw new InvalidOperationException("Unsupported parameter info!");
                                }
                            }
                            else if (reader.NodeType == XmlNodeType.Element)
                            {
                                // Find and read property stored in an element

                                var propInfo = serializableClassInfo.Properties.FirstOrDefault(pi => pi.XmlName == reader.Name && pi.XmlPlacement == Types.XmlPlacement.Element);
                                if (propInfo == null)
                                {
                                    if (config.ErrorOnNotRecognizedProperties)
                                        throw new XmlSerializationException($"Not recognized element {reader.Name} for type {type.Name}");
                                    else
                                    {
                                        // Skip contents of the current property
                                        reader.Skip();
                                        continue;
                                    }
                                }

                                // Try to get a converter for property type

                                var converter = GetConverter(propInfo.Property.PropertyType);

                                object? value = null;

                                if (converter != null)
                                {
                                    // The node is expected to contain text only

                                    if (!reader.Read())
                                        throw new XmlSerializationException("Unexpected end of XML!");
                                    if (reader.NodeType != XmlNodeType.Text)
                                        throw new XmlSerializationException($"Unexpected node in XML: {reader.NodeType} ({reader.Name}) - expected text");

                                    string contents = reader.Value;

                                    // Skip now to end of element to enable skipping of
                                    // incorrectly deserialized values stored in elements

                                    if (!reader.Read())
                                        throw new XmlSerializationException("Unexpected end of XML!");
                                    if (reader.NodeType != XmlNodeType.EndElement)
                                        throw new XmlSerializationException($"Unexpected node in XML: {reader.NodeType} ({reader.Name}) - expected end of element");

                                    // Now try to deserialize

                                    try
                                    {
                                        value = converter.Deserialize(contents);
                                    }
                                    catch (Exception e)
                                    {
                                        if (config.ErrorOnNotRecognizedProperties)
                                            throw new XmlSerializationException($"Failed to deserialize element value {reader.Value} to {propInfo.Property.PropertyType.Name}", e);
                                        else
                                            continue;
                                    }
                                }
                                else
                                {
                                    var propertyType = propInfo.Property.PropertyType;
                                    value = DeserializeExpectedType(propertyType, reader);
                                }

                                // Store deserialized value, depending on kind of property

                                switch (propInfo)
                                {
                                    case ConstructorParameterPropertyInfo constructorParamInfo:
                                        {
                                            while (constructorParameterValues.Count <= constructorParamInfo.Index)
                                                constructorParameterValues.Add(null);

                                            constructorParameterValues[constructorParamInfo.Index] = new ConstructorParameterValue(constructorParamInfo, value);
                                            break;
                                        }
                                    case SimplePropertyInfo simpleProperty:
                                        {
                                            propertyValues.Add(new PropertyValue(simpleProperty, value));
                                            break;
                                        }
                                    default:
                                        throw new InvalidOperationException("Unsupported parameter info!");
                                }
                            }
                            else if (reader.NodeType == XmlNodeType.Text)
                            {
                                nodeContents = reader.Value;
                            }
                            else
                                throw new XmlSerializationException($"Unexpected node in XML: {reader.NodeType} ({reader.Name})");
                        }

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
                                                    .OfType<ConstructorParameterPropertyInfo>()
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

                                    result = Activator.CreateInstance(type, constructorParameterValues.Select(pv => pv.Value).ToArray())!;

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
                default:
                    throw new InvalidOperationException("Unsupported class info!");
            }
        }

        private object? DeserializeExpectedTypes(Type baseType, List<Type> expectedDescendingTypes, XmlReader reader)
        {
            ArgumentNullException.ThrowIfNull(baseType);
            ArgumentNullException.ThrowIfNull(expectedDescendingTypes);
            ArgumentNullException.ThrowIfNull(reader);

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

            if (!reader.Read())
                throw new XmlSerializationException("Unexpected end of XML!");
            if (reader.NodeType != XmlNodeType.Element)
                throw new XmlSerializationException($"Unexpected node: {reader.Name} ({reader.NodeType})");

            int i = classInfos.Count;
            while (i >= 0 && classInfos[i].XmlRoot != reader.Name)
                i--;

            if (i < 0)
                throw new XmlSerializationException($"Node name {reader.Name} does not match any of given types!");

            object? result = DeserializeType(classInfos[i].Type, reader);

            // It is expected that DeserializeType will stop at the closing tag

            return result;
        }

        private object? DeserializeExpectedType(Type expectedType, XmlReader reader)            
        {
            ArgumentNullException.ThrowIfNull(expectedType);
            ArgumentNullException.ThrowIfNull(reader);

            if (!expectedType.IsClass)
                throw new InvalidOperationException("Type passed to DeserializeExpectedType must be a class!");

            return DeserializeExpectedTypes(expectedType, new List<Type> { expectedType }, reader);
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

            var writer = XmlWriter.Create(s);
            Serialize(model, typeof(T), writer);
        }

        public T? Deserialize<T>(Stream s)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(s);
            var reader = XmlReader.Create(s);

            T? result = (T?)DeserializeExpectedType(typeof(T), reader);
            return result;
        }
    }
}
