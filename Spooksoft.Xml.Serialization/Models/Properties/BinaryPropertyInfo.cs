using Spooksoft.Xml.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Models.Properties
{
    internal class BinaryPropertyInfo : BasePropertyInfo
    {
        public BinaryPropertyInfo(
            PropertyInfo property,
            XmlPlacementAttribute? placementAttribute,
            int? constructorParameterIndex) 
            : base(property, placementAttribute, constructorParameterIndex)
        {

        }
    }
}
