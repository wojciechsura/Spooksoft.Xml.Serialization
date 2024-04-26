using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization
{
    internal interface IMapSerializerProvider
    {
        IMapSerializer? GetMapSerializer(Type propertyType);
    }
}
