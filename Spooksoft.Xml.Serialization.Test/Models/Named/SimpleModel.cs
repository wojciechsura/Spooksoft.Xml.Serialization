using Spooksoft.Xml.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Test.Models.Named
{
    [SpkXmlRoot("MySimpleModel")]
    public class SimpleModel
    {
        [SpkXmlElement("MyIntProperty")]
        public int IntProperty { get; set; }

        [SpkXmlAttribute("MyStringProperty")]
        public string? StringProperty { get; set; }
    }
}
