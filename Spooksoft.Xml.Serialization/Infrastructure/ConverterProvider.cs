using System.ComponentModel;
using System;
using System.Globalization;

namespace Spooksoft.Xml.Serialization
{
    namespace Infrastructure
    {              
        internal class ConverterProvider : IConverterProvider
        {
            // Private types --------------------------------------------------

            private sealed class EnumConverter : IConverter
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

            private sealed class NonNullLambdaConverter : IConverter
            {
                private readonly Func<string, object> deserializeFunc;
                private readonly Func<object, string> serializeFunc;

                public NonNullLambdaConverter(Func<object, string> serializeFunc, Func<string, object> deserializeFunc)
                {
                    this.serializeFunc = serializeFunc;
                    this.deserializeFunc = deserializeFunc;
                }

                public object? Deserialize(string value) 
                {
                    return deserializeFunc(value);
                }

                public string Serialize(object? value) 
                {
                    if (value == null)
                        throw new InvalidOperationException("Serializaion of this type from null is not supported");

                    return serializeFunc(value);
                }
            }

            private sealed class NullableLambdaConverter : IConverter
            {
                private readonly Func<string, object?> deserializeFunc;
                private readonly Func<object?, string> serializeFunc;

                public NullableLambdaConverter(Func<object?, string> serializeFunc, Func<string, object?> deserializeFunc)
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
                    { typeof(byte), new NonNullLambdaConverter(b => b.ToString()!, s => byte.Parse(s)) },
                    { typeof(byte?), new NullableLambdaConverter(b => b == null ? "null" : b.ToString()!, s => s.ToLower() == "null" ? (byte?)null : byte.Parse(s)) },
                    { typeof(sbyte), new NonNullLambdaConverter(sb => sb.ToString()!, s => sbyte.Parse(s)) },
                    { typeof(sbyte?), new NullableLambdaConverter(sb => sb == null ? "null" : sb.ToString()!, s => s.ToLower() == "null" ? (sbyte?)null : sbyte.Parse(s)) },
                    { typeof(short), new NonNullLambdaConverter(sh => sh.ToString()!, s => short.Parse(s)) },
                    { typeof(short?), new NullableLambdaConverter(sh => sh == null ? "null" : sh.ToString()!, s => s.ToLower() == "null" ? (short?)null : short.Parse(s)) },
                    { typeof(ushort), new NonNullLambdaConverter(us => us.ToString()!, s => ushort.Parse(s)) },
                    { typeof(ushort?), new NullableLambdaConverter(us => us == null ? "null" : us.ToString()!, s => s.ToLower() == "null" ? (ushort?)null : ushort.Parse(s)) },
                    { typeof(int), new NonNullLambdaConverter(i => i.ToString()!, s => int.Parse(s)) },
                    { typeof(int?), new NullableLambdaConverter(i => i == null ? "null" : i.ToString()!, s => s.ToLower() == "null" ? (int?)null : int.Parse(s)) },
                    { typeof(uint), new NonNullLambdaConverter(ui => ui.ToString()!, s => uint.Parse(s)) },
                    { typeof(uint?), new NullableLambdaConverter(ui => ui == null ? "null" : ui.ToString()!, s => s.ToLower() == "null" ? (uint?)null : uint.Parse(s)) },
                    { typeof(long), new NonNullLambdaConverter(l => l.ToString()!, s => long.Parse(s)) },
                    { typeof(long?), new NullableLambdaConverter(l => l == null ? "null" : l.ToString()!, s => s.ToLower() == "null" ? (long?)null : long.Parse(s)) },
                    { typeof(ulong), new NonNullLambdaConverter(ul => ul.ToString()!, s => ulong.Parse(s)) },
                    { typeof(ulong?), new NullableLambdaConverter(ul => ul == null ? "null" : ul.ToString()!, s => s.ToLower() == "null" ? (ulong?)null : ulong.Parse(s)) },
                    { typeof(float), new NonNullLambdaConverter(f => f.ToString()!, s => float.Parse(s)) },
                    { typeof(float?), new NullableLambdaConverter(f => f == null ? "null" : f.ToString()!, s => s.ToLower() == "null" ? (float?)null : float.Parse(s)) },
                    { typeof(double), new NonNullLambdaConverter(d => d.ToString()!, s => double.Parse(s)) },
                    { typeof(double?), new NullableLambdaConverter(d => d == null ? "null" : d.ToString()!, s => s.ToLower() == "null" ? (double?)null : double.Parse(s)) },
                    { typeof(decimal), new NonNullLambdaConverter(de => de!.ToString()!, s => decimal.Parse(s)) },
                    { typeof(decimal?), new NullableLambdaConverter(d => d == null ? "null" : d.ToString()!, s => s.ToLower() == "null" ? (decimal?)null : decimal.Parse(s)) },
                    { typeof(string), new NullableLambdaConverter(s => ((string?)s) ?? string.Empty, s => s ?? string.Empty) },
                    { typeof(bool), new NonNullLambdaConverter(b => b.ToString()!, s => bool.Parse(s)) },
                    { typeof(bool?), new NullableLambdaConverter(b => b == null ? "null" : b.ToString()!, s => s.ToLower() == "null" ? (bool?)null : bool.Parse(s)) },
                    { typeof(DateTime), new NonNullLambdaConverter(d => ((DateTime)d).ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff"), s => DateTime.ParseExact(s, "yyyy-MM-dd'T'HH:mm:ss.fffffff", CultureInfo.InvariantCulture)) },
                    { typeof(DateTime?), new NullableLambdaConverter(d => d == null ? "null" : ((DateTime)d).ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff"), s => s.ToLower() == "null" ? (DateTime?)null : DateTime.ParseExact(s, "yyyy-MM-dd'T'HH:mm:ss.fffffff", CultureInfo.InvariantCulture)) },
                    { typeof(Guid), new NonNullLambdaConverter(g => g.ToString()!, s => Guid.Parse(s)) },
                    { typeof(Guid?), new NullableLambdaConverter(g => g == null ? "null" : g.ToString()!, s => s.ToLower() == "null" ? (Guid?)null : Guid.Parse(s)) }
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
