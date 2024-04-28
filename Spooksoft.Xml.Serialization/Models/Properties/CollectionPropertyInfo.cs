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

        Type ITypeMappingProperty.CustomTypeAttribute => typeof(XmlArrayItemAttribute);

        // Public methods -----------------------------------------------------

        public CollectionPropertyInfo(PropertyInfo property, 
            XmlPlacementAttribute? placementAttribute, 
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

            (var deserializationMappings, var serializationMappings, _) = 
                BuildMappings(BaseType, "Item", customTypeMappings);

            DeserializationMappings = deserializationMappings;
            SerializationMappings = serializationMappings;
        }

        // Public properties --------------------------------------------------

        public Dictionary<string, Type> DeserializationMappings { get; }
        public Dictionary<Type, string> SerializationMappings { get; }
        public Type BaseType { get; }
    }
}
