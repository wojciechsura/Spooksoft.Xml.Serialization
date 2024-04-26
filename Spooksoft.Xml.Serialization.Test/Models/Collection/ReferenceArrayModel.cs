using Spooksoft.Xml.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Test.Models.Collection
{
    public class ReferenceArrayModel
    {
        [XmlArray]
        [XmlArrayItem("ItemA", typeof(ListItemA))]
        [XmlArrayItem("ItemB", typeof(ListItemB))]
        [XmlArrayItem("BaseItem", typeof(BaseListItem))]
        public BaseListItem?[]? ArrayProp { get; set; }
    }
}
