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
    internal class CollectionPropertyInfo : BasePropertyInfo, ITypeMappingProperty
    {
        // ITypeMappingProperty implementation --------------------------------

        string ITypeMappingProperty.Name => Property.Name;

        Type ITypeMappingProperty.CustomTypeAttribute => typeof(SpkXmlArrayItemAttribute);

        // Public methods -----------------------------------------------------

        public CollectionPropertyInfo(PropertyInfo property, 
            SpkXmlPlacementAttribute? placementAttribute, 
            int? constructorParameterIndex, 
            Dictionary<string, Type> customTypeMappings) 
            : base(property, placementAttribute, constructorParameterIndex)
        {
            if (property.PropertyType.IsArray)
            {
                if (property.PropertyType.GetArrayRank() != 1)
                    throw new XmlModelDefinitionException($"Only single-dimensional arrays are currently supported as collections! Property {property.Name}");

                BaseType = property.PropertyType.GetElementType()!;
            }
            else
            {
                if (!property.PropertyType.IsGenericType || property.PropertyType.GetGenericArguments().Length != 1)
                    throw new XmlModelDefinitionException($"Invalid collection type. Expected single-type-parameter generic type, found {property.PropertyType.Name}!");

                BaseType = property.PropertyType.GetGenericArguments()[0];
            }

            var includeMappings = BaseType.GetCustomAttributes<SpkXmlIncludeDerivedAttribute>()
                .ToDictionary(a => a.Name, a => a.Type);

            if (customTypeMappings != null)
            {
                foreach (var kvp in customTypeMappings)
                {
                    if (includeMappings.ContainsKey(kvp.Key))
                        throw new XmlModelDefinitionException($"Custom type mapping for name {kvp.Key} is already defined in the base type through {nameof(SpkXmlIncludeDerivedAttribute)}. Inspect the type {BaseType.Name} and/or rename custom type mapping in collection.");

                    includeMappings[kvp.Key] = kvp.Value;
                }
            }

            (var deserializationMappings, var serializationMappings, _) = 
                BuildMappings(BaseType, "Item", includeMappings);

            DeserializationMappings = deserializationMappings;
            SerializationMappings = serializationMappings;
        }

        // Public properties --------------------------------------------------

        public Dictionary<string, Type> DeserializationMappings { get; }
        public Dictionary<Type, string> SerializationMappings { get; }
        public Type BaseType { get; }
    }
}
