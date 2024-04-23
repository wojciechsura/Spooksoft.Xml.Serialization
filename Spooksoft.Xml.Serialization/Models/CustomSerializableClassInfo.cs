using Spooksoft.Xml.Serialization.Models.Construction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Models
{
    internal class CustomSerializableClassInfo : BaseClassInfo
    {
        public CustomSerializableClassInfo(ParameterlessCtorConstructionInfo constructor) 
        {
            Constructor = constructor;
        }

        public ParameterlessCtorConstructionInfo Constructor { get; }
    }
}
