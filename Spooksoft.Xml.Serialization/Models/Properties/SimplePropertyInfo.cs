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
    internal class SimplePropertyInfo : BasePropertyInfo, ITypeMappingProperty
    {
        // ITypeMappingProperty implementation --------------------------------

        string ITypeMappingProperty.Name => Property.Name;

        Type ITypeMappingProperty.CustomTypeAttribute => typeof(XmlVariantAttribute);

        // Public methods -----------------------------------------------------

        public SimplePropertyInfo(PropertyInfo property, 
            XmlPlacementAttribute? placementAttribute, 
            int? constructorParameterIndex,
            Dictionary<string, Type> customTypeMappings) 
            : base(property, placementAttribute, constructorParameterIndex)
        {
            var includeMappings = property.PropertyType.GetCustomAttributes<XmlIncludeDerivedAttribute>()
               .ToDictionary(a => a.Name, a => a.Type);

            if (customTypeMappings != null)
            {
                foreach (var kvp in customTypeMappings)
                {
                    if (includeMappings.ContainsKey(kvp.Key))
                        throw new XmlModelDefinitionException($"Custom type mapping for name {kvp.Key} is already defined in the base type through {nameof(XmlIncludeDerivedAttribute)}. Inspect the type {property.PropertyType.Name} and/or rename custom type mapping in collection.");

                    includeMappings[kvp.Key] = kvp.Value;
                }
            }

            (var deserializationMappings, var serializationMappings, var usedCustomMappings) =
                BuildMappings(property.PropertyType, XmlName, includeMappings);

            DeserializationMappings = deserializationMappings;
            SerializationMappings = serializationMappings;
            UsedCustomMappings = usedCustomMappings;
            BaseType = property.PropertyType;
        }

        // Public properties --------------------------------------------------

        public Dictionary<string, Type> DeserializationMappings { get; }
        public Dictionary<Type, string> SerializationMappings { get; }
        public bool UsedCustomMappings { get; }
        public Type BaseType { get; }
    }
}
