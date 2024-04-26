using Spooksoft.Xml.Serialization.Infrastructure.MapSerializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Infrastructure
{
    internal class MapSerializerProvider : IMapSerializerProvider
    {
        private static Dictionary<Type, IMapSerializer> mapSerializers = new()
        {
            { typeof(Dictionary<,>), new DictionarySerializer() }
        };

        public IMapSerializer? GetMapSerializer(Type propertyType)
        {
            if (propertyType.IsGenericType)
            {
                if (mapSerializers.TryGetValue(propertyType.GetGenericTypeDefinition(), out var serializer))
                    return serializer;
            }

            return null;
        }
    }
}
