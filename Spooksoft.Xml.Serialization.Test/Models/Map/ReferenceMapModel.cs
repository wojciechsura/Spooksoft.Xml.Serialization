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
        [SpkXmlMap]
        [SpkXmlMapKey("BaseKey", typeof(BaseKey))]
        [SpkXmlMapKey("DerivedKey", typeof(DerivedKey))]
        [SpkXmlMapValue("BaseValue", typeof(BaseValue))]
        [SpkXmlMapValue("DerivedValue1", typeof(DerivedValue1))]
        [SpkXmlMapValue("DerivedValue2", typeof(DerivedValue2))]
        public Dictionary<BaseKey, BaseValue?>? Dictionary { get; set; }
    }
}
