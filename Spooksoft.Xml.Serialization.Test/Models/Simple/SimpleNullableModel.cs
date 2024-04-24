using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Test.Models.Simple
{
    public class SimpleNullableModel
    {
        public byte? ByteProperty { get; set; } = null;
        public sbyte? SByteProperty { get; set; } = null;
        public short? ShortProperty { get; set; } = null;
        public ushort? UShortProperty { get; set; } = null;
        public int? IntProperty { get; set; } = null;
        public uint? UIntProperty { get; set; } = null;
        public long? LongProperty { get; set; } = null;
        public ulong? ULongProperty { get; set; } = null;
        public bool? BoolProperty { get; set; } = null;
        public float? FloatProperty { get; set; } = null;
        public double? DoubleProperty { get; set; } = null;
        public decimal? DecimalProperty { get; set; } = null;
    }
}
