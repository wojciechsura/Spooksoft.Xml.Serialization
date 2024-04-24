using Spooksoft.Xml.Serialization.Infrastructure.CollectionSerializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Infrastructure
{
    internal class CollectionSerializerProvider : ICollectionSerializerProvider
    {
        private static Dictionary<Type, ICollectionSerializer> collectionSerializers = new()
        {
            { typeof(List<>), new ListSerializer() },
            { typeof(IReadOnlyList<>), new IReadOnlyListSerializer() },
        };

        public ICollectionSerializer? GetCollectionSerializer(Type propertyType)
        {
            if (propertyType.IsGenericType)
            {
                if (collectionSerializers.TryGetValue(propertyType.GetGenericTypeDefinition(), out var serializer))
                    return serializer;
            }

            return null;
        }
    }
}
