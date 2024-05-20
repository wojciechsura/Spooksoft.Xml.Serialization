﻿using Spooksoft.Xml.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Test.Models.Collection
{
    public class ReferenceArrayModel
    {
        [SpkXmlArray]
        [SpkXmlArrayItem("ItemA", typeof(ListItemA))]
        [SpkXmlArrayItem("ItemB", typeof(ListItemB))]
        [SpkXmlArrayItem("BaseItem", typeof(BaseListItem))]
        public BaseListItem?[]? ArrayProp { get; set; }
    }
}
