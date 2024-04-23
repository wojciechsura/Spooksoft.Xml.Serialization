using Spooksoft.Xml.Serialization.Attributes;
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
        public CustomSerializableClassInfo(Type type,
            XmlRootAttribute? rootAttribute, 
            ParameterlessCtorConstructionInfo constructor) 
            : base(type, rootAttribute)
        {
            Constructor = constructor;
        }

        public ParameterlessCtorConstructionInfo Constructor { get; }
    }
}
