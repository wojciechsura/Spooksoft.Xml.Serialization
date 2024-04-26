using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Test.Models.Map
{
    public class DerivedKey : BaseKey
    {
        public override bool Equals(object? obj)
        {
            return (obj is DerivedKey other && other.Index == this.Index);
        }

        public override int GetHashCode()
        {
            return Index.GetHashCode();
        }

        public int Index { get; set; }        
    }
}
