using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Test.Models.CustomMapping
{
    public class DerivedPropertyType2 : BasePropertyType
    {
        public override bool Equals(object? obj)
        {
            return obj?.GetType() == this.GetType();
        }

        public override int GetHashCode()
        {
            return 3;
        }
    }
}
