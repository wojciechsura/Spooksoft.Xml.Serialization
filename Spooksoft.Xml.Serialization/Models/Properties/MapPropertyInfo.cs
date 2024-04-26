using Spooksoft.Xml.Serialization.Attributes;
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
        public MapPropertyInfo(PropertyInfo property,
            XmlPlacementAttribute? placementAttribute,
            int? constructorParameterIndex,
            Dictionary<string, Type> customKeyTypeMappings,
            Dictionary<string, Type> customValueTypeMappings)
            : base(property, placementAttribute, constructorParameterIndex)
        {
            ArgumentNullException.ThrowIfNull(customKeyTypeMappings);
            CustomKeyTypeMappings = customKeyTypeMappings;
            CustomValueTypeMappings = customValueTypeMappings;
        }

        public Dictionary<string, Type> CustomKeyTypeMappings { get; }

        public Dictionary<string, Type> CustomValueTypeMappings { get; }
    }
}
