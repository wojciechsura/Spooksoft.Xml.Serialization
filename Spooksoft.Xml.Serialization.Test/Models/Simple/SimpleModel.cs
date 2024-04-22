using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Test.Models.Simple
{
    public class SimpleModel
    {
        public byte ByteProperty { get; set; } = 0;
        public sbyte SByteProperty { get; set; } = 0;
        public short ShortProperty { get; set; } = 0;
        public ushort UShortProperty { get; set; } = 0;
        public int IntProperty { get; set; } = 0;
        public uint UIntProperty { get; set; } = 0;
        public long LongProperty { get; set; } = 0;
        public ulong ULongProperty { get; set; } = 0;
        public bool BoolProperty { get; set; } = false;
        public string StringProperty { get; set; } = string.Empty;
        public float FloatProperty { get; set; } = 0.0f;
        public double DoubleProperty { get; set; } = 0.0f;
        public decimal DecimalProperty { get; set; } = 0;
    }
}
