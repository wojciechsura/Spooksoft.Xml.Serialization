using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Test.Models.Simple
{
    public class NestedModel
    {
        public SimpleModel? Nested1 { get; set; }
        public SimpleModel? Nested2 { get; set; }
    }
}
