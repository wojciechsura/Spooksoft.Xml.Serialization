using Spooksoft.Xml.Serialization.Conversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization
{
    internal interface IConverterProvider
    {
        IConverter? GetConverter(Type type);
    }
}
