﻿using Spooksoft.Xml.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Test.Models.Simple
{
    public class StringPropModel
    {
        [XmlAttribute("MyString")]
        public string? StringProperty { get; set; }
    }
}
