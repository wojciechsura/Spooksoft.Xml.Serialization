using Spooksoft.Xml.Serialization.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public abstract class SpkXmlPlacementAttribute : Attribute
    {
        protected SpkXmlPlacementAttribute(string name)
        {
            Name = name;
        }

        internal abstract XmlPlacement Placement { get; }

        public string Name { get; }
    }            
}
