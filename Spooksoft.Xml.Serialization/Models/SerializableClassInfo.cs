﻿using Spooksoft.Xml.Serialization.Models.Construction;
using Spooksoft.Xml.Serialization.Models.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Models
{
    internal class SerializableClassInfo : BaseClassInfo
    {
        public SerializableClassInfo(string rootElementName,
            BaseClassConstructionInfo construction,
            IReadOnlyList<BasePropertyInfo> properties)
        {
            RootElementName = rootElementName;
            ConstructionInfo = construction;
            Properties = properties;
        }

        public string RootElementName { get; }
        public BaseClassConstructionInfo ConstructionInfo { get; }
        public IReadOnlyList<BasePropertyInfo> Properties { get; }
    }
}