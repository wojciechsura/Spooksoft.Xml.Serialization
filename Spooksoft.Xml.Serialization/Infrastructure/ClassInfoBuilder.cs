using Spooksoft.Xml.Serialization.Attributes;
using Spooksoft.Xml.Serialization.Exceptions;
using Spooksoft.Xml.Serialization.Models;
using Spooksoft.Xml.Serialization.Models.Construction;
using Spooksoft.Xml.Serialization.Models.Properties;
using Spooksoft.Xml.Serialization.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Infrastructure
{
    internal static class ClassInfoBuilder
    {
        private static ParameterlessClassConstructionInfo? GetParameterlessCtor(Type type)
        {
            // Parameterless constructor
            var ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public, Array.Empty<Type>());

            if (ctor != null)
            {
                return new ParameterlessClassConstructionInfo();
            }
            else
            {
                return null;
            }
        }

        private static (ParameteredClassConstructionInfo? ctor, string? error) GetParameteredCtor(Type type)
        {
            // Parametered ctor must be the only ctor in the type
            var ctors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
            if (ctors.Length > 1)
                return (null, "There is more than one public constructor in the class");
            else if (ctors.Length == 0)
                return (null, "There are no public constructors in the class");

            var ctor = ctors[0];

            // Parametered ctor must have at least one parameter
            ParameterInfo[] ctorParams = ctor.GetParameters();
            if (ctorParams.Length == 0)
                return (null, "The only constructor in the class does not have any parameters");

            // Names of parameters of the parametered ctor must match
            // (case-insensitive) exactly one property each
            List<ConstructorParameterInfo> constructorParameters = new();

            var publicProps = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            for (int i = 0; i < ctorParams.Length; i++)
            {
                ParameterInfo param = ctorParams[i]!;

                var matchingProps = publicProps
                    .Where(p => p.Name.ToLowerInvariant() == param.Name?.ToLowerInvariant())
                    .ToArray();

                // There must be exactly one matching property
                if (matchingProps.Length != 1)
                    return (null, $"There is more than one property ({(string.Join(", ", matchingProps.Select(p => p.Name)))}) matching constructor parameter {param.Name}");

                var matchingProp = matchingProps[0];

                // Types must match exactly
                if (matchingProp.PropertyType != param.ParameterType)
                    return (null, $"Property {matchingProp.Name} and constructor parameter {param.Name} types do not match");

                // Extract XML placement info from the property, if any
                var attributes = matchingProp.GetCustomAttributes();

                var placementAttributes = attributes
                    .OfType<XmlPlacementAttribute>()
                    .ToArray();

                XmlPlacementAttribute? placementAttribute = null;

                if (placementAttributes.Length == 1)
                {
                    placementAttribute = placementAttributes[0];
                }
                else if (placementAttributes.Length > 1)
                    throw new XmlModelDefinitionException($"Property {matchingProp.Name} of class {type.Name} have more than one XML placement attribute!");

                // Check if property does not have XmlIgnore attribute attached
                // That's an error

                XmlIgnoreAttribute? ignoreAttribute = matchingProp.GetCustomAttribute<XmlIgnoreAttribute>();
                if (ignoreAttribute != null)
                    throw new XmlModelDefinitionException($"Property {matchingProp.Name} of class {type.Name} have {nameof(XmlIgnoreAttribute)} attribute attached, but it matches one of constructor's parameters and must be serialized.");

                constructorParameters.Add(new ConstructorParameterInfo(matchingProp, placementAttribute));
            }

            return (new ParameteredClassConstructionInfo(ctor, constructorParameters), null);
        }

        public static BaseClassInfo BuildClassInfo(Type type)
        {
            // 1. Gather some information about the type

            ParameterlessClassConstructionInfo? parameterlessCtor = GetParameterlessCtor(type);

            XmlRootAttribute? xmlRoot = type.GetCustomAttribute<XmlRootAttribute>();

            // 2. Figure out if the type implements IXmlSerializable and 
            //    has a parameterless ctor

            if (type.IsAssignableTo(typeof(IXmlSerializable)) && parameterlessCtor != null)
            {
                return new ClassCustomSerializationInfo(type, xmlRoot, parameterlessCtor);
            }

            // 3. If there is no parameterless ctor, search for parametered one

            ParameteredClassConstructionInfo? parameteredCtor = null;
            string? parameteredCtorError = null;
            if (parameterlessCtor == null)
                (parameteredCtor, parameteredCtorError) = GetParameteredCtor(type);

            if (parameterlessCtor == null && parameteredCtor == null)
                throw new XmlModelDefinitionException($"Class {type.Name} has neither public parameterless constructor nor a public parametered one, which matches the serializer requirements!");

            // 4. Collect information about public properties

            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            List<BasePropertyInfo> typeProperties = new();

            foreach (var property in properties)
            {
                // Check for XmlIgnore

                var xmlIgnore = property.GetCustomAttribute<XmlIgnoreAttribute>();
                if (xmlIgnore != null)
                    continue;

                // Non-readable properties are not supported

                if (!property.CanRead)
                    throw new XmlModelDefinitionException($"Please explicitly mark non-readable (and thus non-serializable) property {property.Name} of class {type.Name} with attribute {nameof(XmlIgnoreAttribute)}");

                // Check for XmlPlacement attribute

                var attributes = property.GetCustomAttributes();
                var placementAttributes = attributes.OfType<XmlPlacementAttribute>().ToArray();
                XmlPlacementAttribute? placementAttribute = null;

                if (placementAttributes.Length == 1)
                    placementAttribute = placementAttributes[0];
                else if (placementAttributes.Length > 1)
                    throw new XmlModelDefinitionException($"Property {property.Name} of class {type.Name} have more than one {nameof(XmlPlacementAttribute)} attribute!");

                // Check if property doesn't match ctor parameter

                ConstructorParameterInfo? matchingCtorParam = null;
                int? matchingCtorParamIndex = null;

                if (parameteredCtor != null)
                {
                    int i = parameteredCtor.ConstructorParameters.Count - 1;

                    while (i >= 0 && parameteredCtor.ConstructorParameters[i].MatchingProperty != property)
                        i--;

                    if (i >= 0)
                    {
                        matchingCtorParam = parameteredCtor.ConstructorParameters[i];
                        matchingCtorParamIndex = i;
                    }
                }

                if ((matchingCtorParam == null || matchingCtorParamIndex == null) && !property.CanWrite)
                    throw new XmlModelDefinitionException($"A read-only property {property.Name} of class {type.Name} can be serialized only if it matches a ctor parameter. If not, explicitly mark it with attribute {nameof(XmlIgnoreAttribute)}");

                // Check if property is a collection

                BasePropertyInfo propInfo;

                var xmlArray = property.GetCustomAttribute<XmlArrayAttribute>();
                if (xmlArray != null)
                {
                    // Serialize property as a collection
                    if (placementAttribute != null && placementAttribute.Placement != XmlPlacement.Element)
                        throw new XmlModelDefinitionException($"Property {property.Name} of class {type.Name} is defined as an {nameof(XmlArrayAttribute)}, which means it can be placed only in an element.");

                    Dictionary<string, Type> customTypeMappings = property.GetCustomAttributes<XmlArrayItemAttribute>()
                        .ToDictionary(a => a.Name, a => a.Type);

                    var collectionProp = new CollectionPropertyInfo(property, placementAttribute, matchingCtorParamIndex, customTypeMappings);

                    if (typeProperties.Exists(tp => tp.MatchesXmlPlacement(collectionProp)))
                        throw new XmlModelDefinitionException($"Two or more properties in type {type.Name} matches XML placement of {collectionProp.XmlPlacement} and name {collectionProp.XmlName}");

                    propInfo = collectionProp;
                }
                else
                {
                    var xmlMap = property.GetCustomAttribute<XmlMapAttribute>();
                    if (xmlMap != null)
                    {
                        if (placementAttribute != null && placementAttribute.Placement != XmlPlacement.Element)
                            throw new XmlModelDefinitionException($"Property {property.Name} of class {type.Name} is defined as an {nameof(XmlMapAttribute)}, which means it can be placed only in an element.");

                        Dictionary<string, Type> customKeyTypeMappings = property.GetCustomAttributes<XmlMapKeyAttribute>()
                            .ToDictionary(a => a.Name, a => a.Type);
                        Dictionary<string, Type> customValueTypeMappings = property.GetCustomAttributes<XmlMapValueAttribute>()
                            .ToDictionary(a => a.Name, a => a.Type);

                        var mapProp = new MapPropertyInfo(property, placementAttribute, matchingCtorParamIndex, customKeyTypeMappings, customValueTypeMappings);

                        if (typeProperties.Exists(tp => tp.MatchesXmlPlacement(mapProp)))
                            throw new XmlModelDefinitionException($"Two or more properties in type {type.Name} matches XML placement of {mapProp.XmlPlacement} and name {mapProp.XmlName}");

                        propInfo = mapProp;
                    }
                    else
                    {
                        // Serialize property as a class

                        var simpleProp = new SimplePropertyInfo(property, placementAttribute, matchingCtorParamIndex);

                        if (typeProperties.Exists(tp => tp.MatchesXmlPlacement(simpleProp)))
                            throw new XmlModelDefinitionException($"Two or more properties in type {type.Name} matches XML placement of {simpleProp.XmlPlacement} and name {simpleProp.XmlName}");

                        propInfo = simpleProp;
                    }
                }

                typeProperties.Add(propInfo);                
            }

            BaseClassConstructionInfo ctor = (BaseClassConstructionInfo?)parameterlessCtor ?? parameteredCtor!;

            return new ClassSerializationInfo(type,
                xmlRoot,
                ctor,
                typeProperties);
        }
    }
}
