using System.ComponentModel;
using System;

namespace Spooksoft.Xml.Serialization
{
    namespace Infrastructure
    {              
        internal class ConverterProvider : IConverterProvider
        {
            // Private types --------------------------------------------------

            private class EnumConverter : IConverter
            {
                private readonly Type type;

                public EnumConverter(Type type)
                {
                    this.type = type;
                }

                public object? Deserialize(string value)
                {
                    return Enum.Parse(type, value);
                }

                public string Serialize(object? value)
                {
                    ArgumentNullException.ThrowIfNull(value);
                    return value.ToString() ?? string.Empty;
                }
            }

            private class LambdaConverter : IConverter
            {
                private readonly Func<string, object?> deserializeFunc;
                private readonly Func<object?, string> serializeFunc;

                public LambdaConverter(Func<object?, string> serializeFunc, Func<string, object?> deserializeFunc)
                {
                    this.serializeFunc = serializeFunc;
                    this.deserializeFunc = deserializeFunc;
                }

                public object? Deserialize(string value) => deserializeFunc(value);
                public string Serialize(object? value) => serializeFunc(value);
            }

            // Private fields ------------------------------------------------

            private static IReadOnlyDictionary<Type, IConverter> converters { get; }

            // Private methods -----------------------------------------------

            private static IConverter GetEnumConverter(Type type)
            {
                return new EnumConverter(type);
            }

            // Static constructor ---------------------------------------------

            static ConverterProvider()
            {
                converters = new Dictionary<Type, IConverter>()
                {
                    { typeof(byte), new LambdaConverter(b => b!.ToString()!, s => byte.Parse(s)) },
                    { typeof(sbyte), new LambdaConverter(sb => sb!.ToString()!, s => sbyte.Parse(s)) },
                    { typeof(short), new LambdaConverter(sh => sh!.ToString()!, s => short.Parse(s)) },
                    { typeof(ushort), new LambdaConverter(us => us!.ToString()!, s => ushort.Parse(s)) },
                    { typeof(int), new LambdaConverter(i => i!.ToString()!, s => int.Parse(s)) },
                    { typeof(uint), new LambdaConverter(ui => ui!.ToString()!, s => uint.Parse(s)) },
                    { typeof(long), new LambdaConverter(l => l!.ToString()!, s => long.Parse(s)) },
                    { typeof(ulong), new LambdaConverter(ul => ul!.ToString()!, s => ulong.Parse(s)) },
                    { typeof(float), new LambdaConverter(f => f!.ToString()!, s => float.Parse(s)) },
                    { typeof(double), new LambdaConverter(d => d!.ToString()!, s => double.Parse(s)) },
                    { typeof(decimal), new LambdaConverter(de => de!.ToString()!, s => decimal.Parse(s)) },
                    { typeof(string), new LambdaConverter(s => ((string?)s) ?? string.Empty, s => s ?? string.Empty) },
                    { typeof(bool), new LambdaConverter(b => b!.ToString()!, s => bool.Parse(s)) }
                };
            }

            // Public methods -------------------------------------------------

            public IConverter? GetConverter(Type type)
            {
                if (type.IsEnum)
                    return GetEnumConverter(type);

                if (converters.TryGetValue(type, out IConverter? result))
                    return result;

                return null;
            }
        }
    }
}
