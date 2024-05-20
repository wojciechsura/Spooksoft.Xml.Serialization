using Spooksoft.Xml.Serialization.Infrastructure.CollectionSerializers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
            { typeof(ImmutableArray<>), new ImmutableArraySerializer() }
        };

        public ICollectionSerializer? GetCollectionSerializer(Type propertyType)
        {
            if (propertyType.IsArray)
            {
                if (propertyType.GetArrayRank() == 1)
                    return new SingleDimensionalArraySerializer();
                else
                    throw new NotImplementedException("Support for multi-dimensional arrays is not implemented");
            }

            if (propertyType.IsGenericType)
            {
                if (collectionSerializers.TryGetValue(propertyType.GetGenericTypeDefinition(), out var serializer))
                    return serializer;
            }

            return null;
        }
    }
}
