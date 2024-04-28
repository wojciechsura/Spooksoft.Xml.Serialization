using Spooksoft.Xml.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Test.Models.CustomMapping
{
    [XmlIncludeDerived("Base", typeof(BasePropertyType))]
    [XmlIncludeDerived("Derived1", typeof(DerivedPropertyType1))]
    // DerivedPropertyType2 is missing on purpose! See CustomMappingModel.
    public class BasePropertyType
    {
        public override bool Equals(object? obj)
        {
            return obj?.GetType() == this.GetType();
        }

        public override int GetHashCode()
        {
            return 1;
        }
    }
}
