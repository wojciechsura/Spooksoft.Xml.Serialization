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
        [SpkXmlArray]
        [SpkXmlArrayItem("ItemA", typeof(ListItemA))]
        [SpkXmlArrayItem("ItemB", typeof(ListItemB))]
        [SpkXmlArrayItem("BaseItem", typeof(BaseListItem))]
        public ImmutableArray<BaseListItem?> ImmutableArray { get; set; }
    }
}
