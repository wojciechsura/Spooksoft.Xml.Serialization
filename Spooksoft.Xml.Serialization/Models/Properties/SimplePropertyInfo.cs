using Spooksoft.Xml.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Models.Properties
{
    internal class SimplePropertyInfo : BasePropertyInfo
    {
        public SimplePropertyInfo(PropertyInfo property, XmlPlacementAttribute? placementAttribute) 
            : base(property, placementAttribute)
        {
        }
    }
}
