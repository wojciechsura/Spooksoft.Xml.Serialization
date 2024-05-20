﻿using Spooksoft.Xml.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Test.Models.Map
{
    public class SimpleMapModel
    {
        [SpkXmlMap]
        public Dictionary<string, int>? Dictionary { get; set; }
    }
}
