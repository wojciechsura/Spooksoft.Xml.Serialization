﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization
{
    internal interface ICollectionSerializerProvider
    {
        ICollectionSerializer? GetCollectionSerializer(Type propertyType);
    }
}
