using Spooksoft.Xml.Serialization.Models.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Spooksoft.Xml.Serialization.Infrastructure.CollectionSerializers
{
    internal class ImmutableArraySerializer : BaseCollectionSerializer, ICollectionSerializer
    {
        public object? Deserialize(Type modelType,
            CollectionPropertyInfo propInfo,
            XmlElement propertyElement,
            XmlDocument document,
            IXmlSerializationProvider provider)
        {
            if (!propInfo.Property.PropertyType.IsGenericType ||
                propInfo.Property.PropertyType.GetGenericTypeDefinition() != typeof(ImmutableArray<>) ||
                propInfo.Property.PropertyType.GenericTypeArguments.Length != 1)
                throw new InvalidOperationException($"${nameof(ImmutableArraySerializer)} can be called only for property of type ImmutableArray<T>!");

            Type arrayMemberType = propInfo.Property.PropertyType.GenericTypeArguments[0];
            
            // ImmutableArray<T>
            var immutableArrayType = typeof(ImmutableArray<>)
                .MakeGenericType(arrayMemberType);

            // ImmutableArray<T>.CreateBuilder()
            var createBuilderMethod = typeof(ImmutableArray)
                .GetMethod(nameof(ImmutableArray.CreateBuilder), BindingFlags.Public | BindingFlags.Static, Array.Empty<Type>())!
                .MakeGenericMethod(arrayMemberType);

            // ImmutableArray<T>.Builder
            var builderType = immutableArrayType
                .GetNestedType(nameof(ImmutableArray<object>.Builder))!
                .MakeGenericType(propInfo.Property.PropertyType.GenericTypeArguments[0])!;

            // ImmutableArray<T>.Builder.Add()
            var builderAddMethod = builderType
                .GetMethod(nameof(ImmutableArray<object>.Builder.Add), BindingFlags.Instance | BindingFlags.Public)!;

            // ImmutableArray<T>.Builder.ToImmutable()
            var builderBuildMethod = builderType
                .GetMethod(nameof(ImmutableArray<object>.Builder.ToImmutable), BindingFlags.Instance | BindingFlags.Public)!;

            object builderConstructor()
            {
                return createBuilderMethod.Invoke(null, null)!;
            }

            void itemAdder(object collection, object? item)
            {
                builderAddMethod.Invoke(collection, new[] { item });
            }

            var builder = DeserializeCollection(modelType,
                propInfo,
                propertyElement,
                document,
                provider,
                builderConstructor,
                itemAdder);

            if (builder == null)
                return null;

            return builderBuildMethod.Invoke(builder, null);
        }

        public void Serialize(object? collection,
            Type modelType,
            CollectionPropertyInfo propInfo,
            XmlElement propertyElement,
            XmlDocument document,
            IXmlSerializationProvider provider)
        {
            if (!propInfo.Property.PropertyType.IsGenericType ||
                propInfo.Property.PropertyType.GetGenericTypeDefinition() != typeof(ImmutableArray<>))
                throw new InvalidOperationException($"${nameof(ImmutableArraySerializer)} can be called only for property of type ImmutableArray<T>!");

            SerializeAsIEnumerable(collection, modelType, propInfo, propertyElement, document, provider);
        }
    }
}
