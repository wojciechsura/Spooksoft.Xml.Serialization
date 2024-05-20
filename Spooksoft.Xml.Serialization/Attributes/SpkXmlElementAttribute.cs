using Spooksoft.Xml.Serialization.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SpkXmlElementAttribute : SpkXmlPlacementAttribute
    {
        public SpkXmlElementAttribute(string name)
            : base(name)
        {
            
        }

        internal override XmlPlacement Placement => XmlPlacement.Element;
    }
}
