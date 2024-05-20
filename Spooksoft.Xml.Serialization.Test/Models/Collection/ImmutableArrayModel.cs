using Spooksoft.Xml.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Test.Models.Collection
{
    public class ImmutableArrayModel
    {
        [XmlArray]
        [XmlArrayItem("ItemA", typeof(ListItemA))]
        [XmlArrayItem("ItemB", typeof(ListItemB))]
        [XmlArrayItem("BaseItem", typeof(BaseListItem))]
        public ImmutableArray<BaseListItem?> ImmutableArray { get; set; }
    }
}
