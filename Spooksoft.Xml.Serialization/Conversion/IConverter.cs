using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Conversion
{
    public interface IConverter
    {
        string Serialize(object value);
        object Deserialize(string value);
    }
}
