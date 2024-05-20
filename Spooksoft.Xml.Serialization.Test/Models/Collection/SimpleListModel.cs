using Spooksoft.Xml.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Test.Models.Collection
{
    public class SimpleListModel
    {
        [SpkXmlArray]
        [SpkXmlElement("MyList")]
        public List<string?>? Strings { get; set; }
    }
}
