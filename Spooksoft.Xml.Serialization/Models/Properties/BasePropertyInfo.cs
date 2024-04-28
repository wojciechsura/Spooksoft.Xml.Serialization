using Spooksoft.Xml.Serialization.Attributes;
using Spooksoft.Xml.Serialization.Exceptions;
using Spooksoft.Xml.Serialization.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Models.Properties
{
    internal abstract class BasePropertyInfo
    {
        protected BasePropertyInfo(PropertyInfo property, 
            XmlPlacementAttribute? placementAttribute,
            int? constructorParameterIndex)
        {
            Property = property;
            PlacementAttribute = placementAttribute;
            ConstructorParameterIndex = constructorParameterIndex;
        }

        protected static (Dictionary<string, Type> deserializationMappings, Dictionary<Type, string> serializationMappings, bool usedCustomMappings) BuildMappings(
            Type baseType,
            string baseName,
            Dictionary<string, Type> customTypeMappings)
        {
            Dictionary<string, Type> deserializationMappings;
            Dictionary<Type, string> serializationMappings;
            bool usedCustomMappings;

            if (customTypeMappings != null && customTypeMappings.Any())
            {
                usedCustomMappings = true;

                deserializationMappings = customTypeMappings;

                serializationMappings = new();

                foreach (var mapping in customTypeMappings)
                {
                    // Validate mappings

                    if (serializationMappings.ContainsKey(mapping.Value))
                        throw new XmlInvalidTypeMappingsException($"Invalid custom mappings: there are multiple mappings of type {mapping.Value}. Note that mappings may be as well defined for the type {baseType.Name} with {nameof(XmlIncludeDerivedAttribute)} attribute.");
                    if (!mapping.Value.IsAssignableTo(baseType))
                        throw new XmlModelDefinitionException($"Invalid type mapping. Type {mapping.Value.Name} is not assignable to {baseType.Name}!");

                    serializationMappings[mapping.Value] = mapping.Key;
                }
            }
            else
            {
                usedCustomMappings = false;

                deserializationMappings = new()
                {
                    { baseName, baseType }
                };
                serializationMappings = new()
                {
                    { baseType, baseName }
                };
            }

            return (deserializationMappings, serializationMappings, usedCustomMappings);
        }

        /// <summary>
        /// Checks if the given property matches the XML placement
        /// (has the same XML name and placement: element or attribute)
        /// </summary>
        public bool MatchesXmlPlacement(BasePropertyInfo other)
        {
            return this.XmlName == other.XmlName && this.XmlPlacement == other.XmlPlacement;
        }

        public PropertyInfo Property { get; }

        public XmlPlacementAttribute? PlacementAttribute { get; }

        public int? ConstructorParameterIndex { get; }

        public string XmlName => PlacementAttribute != null ? PlacementAttribute.Name : Property.Name;

        public XmlPlacement XmlPlacement => PlacementAttribute != null ? PlacementAttribute.Placement : XmlPlacement.Element;
    }
}
