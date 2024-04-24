using Spooksoft.Xml.Serialization.Conversion;

namespace Spooksoft.Xml.Serialization
{
    namespace Infrastructure
    {
        internal class ConverterProvider : IConverterProvider
        {
            public IConverter? GetConverter(Type type)
            {
                if (type.IsEnum)
                    return DefaultConverters.GetEnumConverter(type);

                if (DefaultConverters.Converters.TryGetValue(type, out IConverter? result))
                    return result;

                return null;
            }
        }
    }
}
