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
            int? constructorParameterIndex) 
            : base(property, placementAttribute, constructorParameterIndex)
        {

        }
    }
}
