using Spooksoft.Xml.Serialization.Attributes;
using Spooksoft.Xml.Serialization.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        private MappingPropertyProxy keyProxy;
        private MappingPropertyProxy valueProxy;

        public MapPropertyInfo(PropertyInfo property,
            XmlPlacementAttribute? placementAttribute,
            int? constructorParameterIndex,
            Dictionary<string, Type> customKeyTypeMappings,
            Dictionary<string, Type> customValueTypeMappings)
            : base(property, placementAttribute, constructorParameterIndex)
        {
            if (!property.PropertyType.IsGenericType || property.PropertyType.GetGenericArguments().Length != 2)
                throw new XmlModelDefinitionException($"Invalid map type. Expected double-type-parameter generic type, found {property.PropertyType.Name}!");

            KeyType = property.PropertyType.GetGenericArguments()[0];
            ValueType = property.PropertyType.GetGenericArguments()[1];

            (var deserializationKeyMappings, var serializationKeyMappings, _) =
                BuildMappings(KeyType, "Data", customKeyTypeMappings);
            DeserializationKeyTypeMappings = deserializationKeyMappings;
            SerializationKeyTypeMappings = serializationKeyMappings;

            (var deserializationValueMappings, var serializationValueMappings, _) =
                BuildMappings(ValueType, "Data", customValueTypeMappings);
            DeserializationValueTypeMappings = deserializationValueMappings;
            SerializationValueTypeMappings = serializationValueMappings;

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
