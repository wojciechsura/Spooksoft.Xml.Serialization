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
        [SpkXmlVariant("Derived2", typeof(DerivedPropertyType2))]
        public BasePropertyType? Property1 { get; set; }

        [SpkXmlVariant("Derived2", typeof(DerivedPropertyType2))]
        public BasePropertyType? Property2 { get; set; }

        [SpkXmlVariant("Derived2", typeof(DerivedPropertyType2))]
        public BasePropertyType? Property3 { get; set; }

        [SpkXmlArray]
        [SpkXmlArrayItem("Derived2", typeof(DerivedPropertyType2))]        
        public List<BasePropertyType?>? List { get; set; }

        [SpkXmlMap]
        [SpkXmlMapKey("Derived2", typeof(DerivedPropertyType2))]
        [SpkXmlMapValue("Derived2", typeof(DerivedPropertyType2))]
        public Dictionary<BasePropertyType, BasePropertyType?>? Dictionary { get; set; }
    }
}
