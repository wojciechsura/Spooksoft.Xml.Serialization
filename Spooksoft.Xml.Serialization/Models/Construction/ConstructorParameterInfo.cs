using Spooksoft.Xml.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Models.Construction
{
    internal class ConstructorParameterInfo
    {
        public ConstructorParameterInfo(PropertyInfo matchingProperty, XmlPlacementAttribute? xmlPlacement)
        {
            MatchingProperty = matchingProperty;
            XmlPlacement = xmlPlacement;
        }

        public PropertyInfo MatchingProperty { get; }
        public XmlPlacementAttribute? XmlPlacement { get; }
    }
}
