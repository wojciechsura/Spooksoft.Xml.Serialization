using Spooksoft.Xml.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Test.Models.Collection
{
    public class SimpleArrayModel
    {
        [XmlArray]
        public int[]? ArrayProp { get; set; }
    }
}
