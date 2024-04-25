using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization
{
    public class XmlSerializerConfig
    {
        public bool UseSharedTypeCache { get; set; } = false;
        public bool ErrorOnNotRecognizedProperties { get; set; } = true;
        public bool ErrorOnFailedPropertyDeserialization { get; set; } = true;
        public bool ReplaceMissingCtorParamsWithDefaultValues { get; set; } = false;
    }
}
