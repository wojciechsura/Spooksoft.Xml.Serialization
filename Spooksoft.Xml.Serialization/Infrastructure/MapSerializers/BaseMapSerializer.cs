using Spooksoft.Xml.Serialization.Attributes;
using Spooksoft.Xml.Serialization.Exceptions;
using Spooksoft.Xml.Serialization.Models.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Infrastructure.MapSerializers
{
    internal abstract class BaseMapSerializer
    {
        protected (Type keyType, Type valueType, Dictionary<string, Type> keyMappings, Dictionary<string, Type> valueMappings) GetMappings(MapPropertyInfo propInfo, IConverterProvider converterProvider, IClassSerializationInfoProvider classInfoProvider)
        {
            Type keyType, valueType;

            if (propInfo.Property.PropertyType.IsGenericType)
            {
                if (propInfo.Property.PropertyType.GetGenericArguments().Length != 2)
                    throw new InvalidOperationException($"{nameof(GetMappings)} method supports only single-type-parameter generic types!");

                keyType = propInfo.Property.PropertyType.GetGenericArguments()[0];
                valueType = propInfo.Property.PropertyType.GetGenericArguments()[1];
            }
            else
                throw new InvalidOperationException("Unsupported map type!");

            Dictionary<string, Type> BuildMappings(Dictionary<string, Type> existingMappings, 
                Type baseType, 
                string mappingType,
                Type attributeType)
            {
                var mappings = existingMappings;

                if (!mappings.Any())
                {
                    var converter = converterProvider.GetConverter(baseType);
                    if (converter != null)
                        mappings.Add("Data", baseType);
                    else
                    {
                        var classInfo = classInfoProvider.GetClassInfo(baseType);
                        mappings.Add(classInfo.XmlRoot, baseType);
                    }
                }

                foreach (var mapping in mappings.Where(mapping => !mapping.Value.IsAssignableTo(baseType)))
                    throw new XmlModelDefinitionException($"Invalid {mappingType} type mapping for {attributeType.Name}. Type {mapping.Value.Name} is not assignable to {baseType.Name}!");

                return mappings;
            }

            var keyMappings = BuildMappings(propInfo.CustomKeyTypeMappings, keyType, "key", typeof(XmlMapKeyAttribute));
            var valueMappings = BuildMappings(propInfo.CustomValueTypeMappings, valueType, "value", typeof(XmlMapValueAttribute));

            return (keyType, valueType, keyMappings, valueMappings);
        }

        protected (Type keyType, Type valueType, Dictionary<Type, string> reversedKeyMappings, Dictionary<Type, string> reversedValueMappings) GetReverseMappings(MapPropertyInfo propInfo, IConverterProvider converterProvider, IClassSerializationInfoProvider classInfoProvider)
        {
            (var keyType, var valueType, var keyMappings, var valueMappings) = GetMappings(propInfo, converterProvider, classInfoProvider);

            Dictionary<Type, string> reversedKeyMappings = new();
            foreach (var kvp in keyMappings)
                reversedKeyMappings[kvp.Value] = kvp.Key;

            Dictionary<Type, string> reversedValueMappings = new();
            foreach (var kvp in valueMappings)
                reversedValueMappings[kvp.Value] = kvp.Key;

            return (keyType, valueType, reversedKeyMappings, reversedValueMappings);
        }
    }
}
