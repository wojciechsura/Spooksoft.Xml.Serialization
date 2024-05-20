using Spooksoft.Xml.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Test.Models.Simple
{
    public class VaryingPropertyTypeModel
    {
        [SpkXmlVariant("Base", typeof(BasePropertyType))]
        [SpkXmlVariant("Derived1", typeof(DerivedPropertyType1))]
        [SpkXmlVariant("Derived2", typeof(DerivedPropertyType2))]
        public BasePropertyType? MyProperty { get; set; }
    }
}
