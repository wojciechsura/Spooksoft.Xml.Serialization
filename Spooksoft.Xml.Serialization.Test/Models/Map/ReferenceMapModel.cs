using Spooksoft.Xml.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Test.Models.Map
{
    public class ReferenceMapModel
    {
        [XmlMap]
        [XmlMapKey("BaseKey", typeof(BaseKey))]
        [XmlMapKey("DerivedKey", typeof(DerivedKey))]
        [XmlMapValue("BaseValue", typeof(BaseValue))]
        [XmlMapValue("DerivedValue1", typeof(DerivedValue1))]
        [XmlMapValue("DerivedValue2", typeof(DerivedValue2))]
        public Dictionary<BaseKey, BaseValue?>? Dictionary { get; set; }
    }
}
