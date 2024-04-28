using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization
{
    internal interface ITypeMappingProperty
    {
        string Name { get; }
        Type BaseType { get; }
        Dictionary<Type, string> SerializationMappings { get; }
        Dictionary<string, Type> DeserializationMappings { get; }
        Type CustomTypeAttribute { get; }
    }
}
