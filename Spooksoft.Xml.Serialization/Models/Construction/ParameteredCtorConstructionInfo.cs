﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Models.Construction
{
    internal class ParameteredCtorConstructionInfo : BaseClassConstructionInfo
    {
        public ParameteredCtorConstructionInfo(ConstructorInfo constructor, IReadOnlyList<ConstructorParameterInfo> constructorParameters)
        {
            Constructor = constructor;
            ConstructorParameters = constructorParameters;
        }

        public ConstructorInfo Constructor { get; }
        public IReadOnlyList<ConstructorParameterInfo> ConstructorParameters { get; }
    }
}
