using Spooksoft.Xml.Serialization.Attributes;
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
            (var deserializationMappings, var serializationMappings, var usedCustomMappings) =
                BuildMappings(property.PropertyType, XmlName, customTypeMappings);

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
