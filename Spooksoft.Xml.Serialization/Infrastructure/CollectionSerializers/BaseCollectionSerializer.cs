using Spooksoft.Xml.Serialization.Attributes;
using Spooksoft.Xml.Serialization.Common;
using Spooksoft.Xml.Serialization.Exceptions;
using Spooksoft.Xml.Serialization.Infrastructure.CollectionSerializers;
using Spooksoft.Xml.Serialization.Models.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Spooksoft.Xml.Serialization.Infrastructure.CollectionSerializers
{
    internal abstract class BaseCollectionSerializer
    {

        protected object? DeserializeCollection(Type modelType,
            CollectionPropertyInfo propInfo,
            XmlElement propertyElement,
            XmlDocument document,
            IXmlSerializationProvider provider,
            Func<object> typeConstructor,
            Action<object, object?> addItem)
        {
            // Null list

            var listNullAttribute = propertyElement.Attributes
                .OfType<XmlAttribute>()
                .FirstOrDefault(a => a.NamespaceURI == Constants.CONTROL_NAMESPACE_URI && a.LocalName == Constants.NIL_ATTRIBUTE);

            if (listNullAttribute != null && listNullAttribute.Value.ToLower() == "true")
            {
                return null;
            }

            // Empty list

            var result = typeConstructor()!;

            // Items

            foreach (var node in propertyElement.ChildNodes.OfType<XmlElement>())
            {
                object? deserializedItem = provider.DeserializePropertyValue(modelType, node, propInfo, document);
                addItem(result, deserializedItem);
            }

            return result;
        }

        protected IList? DeserializeIList(Type modelType,
            CollectionPropertyInfo propInfo,
            XmlElement propertyElement,
            XmlDocument document,
            IXmlSerializationProvider provider,
            Func<IList> typeConstructor) =>
            (IList?)DeserializeCollection(modelType, propInfo, propertyElement, document, provider, typeConstructor, (obj, item) => ((IList)obj).Add(item));

        protected void SerializeAsIEnumerable(object? collection, Type modelType, CollectionPropertyInfo propInfo, XmlElement propertyElement, XmlDocument document, IXmlSerializationProvider provider)
        {
            // Null collection

            if (collection == null)
            {
                propertyElement.SetAttribute(Constants.NIL_ATTRIBUTE, Constants.CONTROL_NAMESPACE_URI, "true");
                return;
            }

            // Empty collection

            IEnumerable enumerable = (IEnumerable)collection;

            // Items

            foreach (var item in enumerable)
                provider.SerializePropertyValue(modelType, propertyElement, item, propInfo, document);
        }
    }
}
