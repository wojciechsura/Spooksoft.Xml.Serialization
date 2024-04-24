using Spooksoft.Xml.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Test.Models.Simple
{
    public class ExtendedTypesModel
    {
        [XmlAttribute("DateTime")]
        public DateTime DateTime { get; set; }
        [XmlAttribute("NullableDateTime")]
        public DateTime? NullableDateTime { get; set; }
        public Guid Guid { get; set; }
        public Guid? NullableGuid { get; set; }
    }
}
