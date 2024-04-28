using Spooksoft.Xml.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Test.Models.CustomMapping
{
    public class CustomMappingModel
    {
        [XmlVariant("Derived2", typeof(DerivedPropertyType2))]
        public BasePropertyType? Property1 { get; set; }

        [XmlVariant("Derived2", typeof(DerivedPropertyType2))]
        public BasePropertyType? Property2 { get; set; }

        [XmlVariant("Derived2", typeof(DerivedPropertyType2))]
        public BasePropertyType? Property3 { get; set; }

        [XmlArray]
        [XmlArrayItem("Derived2", typeof(DerivedPropertyType2))]        
        public List<BasePropertyType?>? List { get; set; }

        [XmlMap]
        [XmlMapKey("Derived2", typeof(DerivedPropertyType2))]
        [XmlMapValue("Derived2", typeof(DerivedPropertyType2))]
        public Dictionary<BasePropertyType, BasePropertyType?>? Dictionary { get; set; }
    }
}
