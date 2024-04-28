using Spooksoft.Xml.Serialization.Attributes;
using Spooksoft.Xml.Serialization.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Models.Properties
{
    internal class MapPropertyInfo : BasePropertyInfo
    {
        private class MappingPropertyProxy : ITypeMappingProperty
        {
            public MappingPropertyProxy(string name,
                Type baseType,
                Dictionary<Type, string> serializationMappings,
                Dictionary<string, Type> deserializationMappings,
                Type customTypeAttribute)
            {
                Name = name;
                BaseType = baseType;
                SerializationMappings = serializationMappings;
                DeserializationMappings = deserializationMappings;
                CustomTypeAttribute = customTypeAttribute;
            }

            public string Name { get; }

            public Type BaseType { get; }

            public Dictionary<Type, string> SerializationMappings { get; }

            public Dictionary<string, Type> DeserializationMappings { get; }

            public Type CustomTypeAttribute { get; }
        }

        private readonly MappingPropertyProxy keyProxy;
        private readonly MappingPropertyProxy valueProxy;

        public MapPropertyInfo(PropertyInfo property,
            XmlPlacementAttribute? placementAttribute,
            int? constructorParameterIndex,
            Dictionary<string, Type> customKeyTypeMappings,
            Dictionary<string, Type> customValueTypeMappings)
            : base(property, placementAttribute, constructorParameterIndex)
        {
            if (!property.PropertyType.IsGenericType || property.PropertyType.GetGenericArguments().Length != 2)
                throw new XmlModelDefinitionException($"Invalid map type. Expected double-type-parameter generic type, found {property.PropertyType.Name}!");

            // Key type mappings

            KeyType = property.PropertyType.GetGenericArguments()[0];

            var keyIncludeMappings = KeyType.GetCustomAttributes<XmlIncludeDerivedAttribute>()
               .ToDictionary(a => a.Name, a => a.Type);

            if (customKeyTypeMappings != null)
            {
                foreach (var kvp in customKeyTypeMappings)
                {
                    if (keyIncludeMappings.ContainsKey(kvp.Key))
                        throw new XmlModelDefinitionException($"Custom key type mapping for name {kvp.Key} is already defined in the base type through {nameof(XmlIncludeDerivedAttribute)}. Inspect the type {KeyType.Name} and/or rename custom type mapping in collection.");

                    keyIncludeMappings[kvp.Key] = kvp.Value;
                }
            }

            (var deserializationKeyMappings, var serializationKeyMappings, _) =
                BuildMappings(KeyType, "Data", keyIncludeMappings);
            DeserializationKeyTypeMappings = deserializationKeyMappings;
            SerializationKeyTypeMappings = serializationKeyMappings;

            // Value type mappings

            ValueType = property.PropertyType.GetGenericArguments()[1];

            var valueIncludeMappings = ValueType.GetCustomAttributes<XmlIncludeDerivedAttribute>()
                           .ToDictionary(a => a.Name, a => a.Type);

            if (customValueTypeMappings != null)
            {
                foreach (var kvp in customValueTypeMappings)
                {
                    if (valueIncludeMappings.ContainsKey(kvp.Key))
                        throw new XmlModelDefinitionException($"Custom value type mapping for name {kvp.Key} is already defined in the base type through {nameof(XmlIncludeDerivedAttribute)}. Inspect the type {ValueType.Name} and/or rename custom type mapping in collection.");

                    valueIncludeMappings[kvp.Key] = kvp.Value;
                }
            }

            (var deserializationValueMappings, var serializationValueMappings, _) =
                BuildMappings(ValueType, "Data", valueIncludeMappings);
            DeserializationValueTypeMappings = deserializationValueMappings;
            SerializationValueTypeMappings = serializationValueMappings;

            // Proxies

            keyProxy = new MappingPropertyProxy(property.Name, KeyType, SerializationKeyTypeMappings, DeserializationKeyTypeMappings, typeof(XmlMapKeyAttribute));
            valueProxy = new MappingPropertyProxy(property.Name, ValueType, SerializationValueTypeMappings, DeserializationValueTypeMappings, typeof(XmlMapValueAttribute));
        }

        public Dictionary<string, Type> DeserializationKeyTypeMappings { get; }
        public Dictionary<string, Type> DeserializationValueTypeMappings { get; }
        public Dictionary<Type, string> SerializationKeyTypeMappings { get; }
        public Dictionary<Type, string> SerializationValueTypeMappings { get; }
        public Type KeyType { get; }
        public Type ValueType { get; }

        public ITypeMappingProperty KeyMappingProperty => keyProxy;
        public ITypeMappingProperty ValueMappingProperty => valueProxy;
    }
}
