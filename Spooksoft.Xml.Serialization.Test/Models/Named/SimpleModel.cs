using Spooksoft.Xml.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Test.Models.Named
{
    [XmlRoot("MySimpleModel")]
    public class SimpleModel
    {
        [XmlElement("MyIntProperty")]
        public int IntProperty { get; set; }

        [XmlAttribute("MyStringProperty")]
        public string? StringProperty { get; set; }
    }
}
