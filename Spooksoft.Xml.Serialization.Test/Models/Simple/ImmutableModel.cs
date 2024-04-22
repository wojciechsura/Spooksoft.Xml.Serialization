using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Test.Models.Simple
{
    public class ImmutableModel
    {
        public ImmutableModel(int intProperty, string stringProperty)
        {
            IntProperty = intProperty;
            StringProperty = stringProperty;
        }

        public int IntProperty { get; }
        public string StringProperty { get; }
    }
}
