using Spooksoft.Xml.Serialization.Models.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Spooksoft.Xml.Serialization.Infrastructure.CollectionSerializers
{
    internal class SingleDimensionalArraySerializer : BaseCollectionSerializer, ICollectionSerializer
    {
        public object? Deserialize(Type modelType, 
            CollectionPropertyInfo propInfo, 
            XmlElement propertyElement, 
            XmlDocument document, 
            IXmlSerializationProvider provider)
        {
            if (!propInfo.Property.PropertyType.IsArray)
                throw new InvalidOperationException($"${nameof(SingleDimensionalArraySerializer)} can be called only for property of type T[]!");
            if (propInfo.Property.PropertyType.GetArrayRank() != 1)
                throw new InvalidOperationException($"${nameof(SingleDimensionalArraySerializer)} can be called only for single-dimensional array T[]!");

            // First, deserialize items to list (we don't know
            // how many of them are there)

            IList listActivator()
            {
                var listType = typeof(List<>).MakeGenericType(propInfo.Property.PropertyType.GetElementType()!);
                return (IList)Activator.CreateInstance(listType)!;
            }

            IList? items = DeserializeIList(modelType, propInfo, propertyElement, document, provider, listActivator);

            // If collection is null, return null

            if (items == null)
                return null;

            // Construct the result array

            var result = Array.CreateInstance(propInfo.Property.PropertyType.GetElementType()!, items.Count);

            // Copy all items to the result array

            for (int i = 0; i < items.Count; i++)
                ((IList)result)[i] = items[i];

            return result;
        }

        public void Serialize(object? collection, 
            Type modelType, 
            CollectionPropertyInfo propInfo, 
            XmlElement propertyElement, 
            XmlDocument document, 
            IXmlSerializationProvider provider)
        {
            if (!propInfo.Property.PropertyType.IsArray)
                throw new InvalidOperationException($"${nameof(SingleDimensionalArraySerializer)} can be called only for property of type T[]!");
            if (propInfo.Property.PropertyType.GetArrayRank() != 1)
                throw new InvalidOperationException($"${nameof(SingleDimensionalArraySerializer)} can be called only for single-dimensional array T[]!");

            SerializeAsIEnumerable(collection, modelType, propInfo, propertyElement, document, provider);
        }
    }
}
