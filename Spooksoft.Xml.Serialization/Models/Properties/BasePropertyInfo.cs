using Spooksoft.Xml.Serialization.Attributes;
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
