using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Conversion
{
    internal class LambdaConverter : IConverter
    {
        private readonly Func<string, object> deserializeFunc;
        private readonly Func<object, string> serializeFunc;

        public LambdaConverter(Func<object, string> serializeFunc, Func<string, object> deserializeFunc)
        {
            this.serializeFunc = serializeFunc;
            this.deserializeFunc = deserializeFunc;
        }

        public object Deserialize(string value) => deserializeFunc(value);
        public string Serialize(object value) => serializeFunc(value);
    }
}
