using Spooksoft.Xml.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Models.Properties
{
    internal class ConstructorParameterPropertyInfo : BasePropertyInfo
    {
        public ConstructorParameterPropertyInfo(int index, PropertyInfo property, XmlPlacementAttribute? placementAttribute)
            : base(property, placementAttribute)
        {
            Index = index;
        }

        public int Index { get; }
    }
}
