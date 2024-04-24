using Spooksoft.Xml.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Models.Properties
{
    internal class CollectionPropertyInfo : BasePropertyInfo
    {
        public CollectionPropertyInfo(PropertyInfo property, 
            XmlPlacementAttribute? placementAttribute, 
            int? constructorParameterIndex, 
            Dictionary<string, Type> customTypeMappings) 
            : base(property, placementAttribute, constructorParameterIndex)
        {
            ArgumentNullException.ThrowIfNull(customTypeMappings);
            CustomTypeMappings = customTypeMappings;
        }

        public Dictionary<string, Type> CustomTypeMappings { get; }
    }
}
