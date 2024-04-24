using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Spooksoft.Xml.Serialization
{
    internal interface IXmlSerializationProvider
    {
        IConverterProvider ConverterProvider { get; }
        IClassInfoProvider ClassInfoProvider { get; }

        object? DeserializeExpectedType(Type expectedType, string elementName, XmlElement element, XmlDocument document);
        object? DeserializeExpectedTypes(Type baseType, Dictionary<string, Type> expectedDescendingTypes, XmlElement element, XmlDocument document);
        XmlElement SerializeObjectToElement(object? model, Type modelType, string elementName, XmlDocument document);
    }
}
