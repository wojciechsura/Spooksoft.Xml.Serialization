using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Conversion
{
    internal static class DefaultConverters
    {
        private class EnumConverter : IConverter
        {
            private readonly Type type;

            public EnumConverter(Type type)
            {
                this.type = type;
            }

            public object Deserialize(string value)
            {
                return Enum.Parse(type, value);
            }

            public string Serialize(object value)
            {
                return value.ToString() ?? string.Empty;
            }
        }

        static DefaultConverters()
        {
            Converters = new Dictionary<Type, IConverter>()
            {
                { typeof(byte), new LambdaConverter(b => b.ToString()!, s => byte.Parse(s)) },
                { typeof(sbyte), new LambdaConverter(sb => sb.ToString()!, s => sbyte.Parse(s)) },
                { typeof(short), new LambdaConverter(sh => sh.ToString()!, s => short.Parse(s)) },
                { typeof(ushort), new LambdaConverter(us => us.ToString()!, s => ushort.Parse(s)) },
                { typeof(int), new LambdaConverter(i => i.ToString()!, s => int.Parse(s)) },
                { typeof(uint), new LambdaConverter(ui => ui.ToString()!, s => uint.Parse(s)) },
                { typeof(long), new LambdaConverter(l => l.ToString()!, s => long.Parse(s)) },
                { typeof(ulong), new LambdaConverter(ul => ul.ToString()!, s => ulong.Parse(s)) },
                { typeof(float), new LambdaConverter(f => f.ToString()!, s => float.Parse(s)) },
                { typeof(double), new LambdaConverter(d => d.ToString()!, s => double.Parse(s)) },
                { typeof(decimal), new LambdaConverter(de => de.ToString()!, s => decimal.Parse(s)) },
                { typeof(string), new LambdaConverter(s => (string)s, s => s) },
                { typeof(bool), new LambdaConverter(b => b.ToString()!, s => bool.Parse(s)) }
            };            
        }

        internal static IReadOnlyDictionary<Type, IConverter> Converters { get; }

        internal static IConverter GetEnumConverter(Type type)
        {
            return new EnumConverter(type);
        }
    }
}
