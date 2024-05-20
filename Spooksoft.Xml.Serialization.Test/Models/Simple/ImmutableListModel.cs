using Spooksoft.Xml.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Test.Models.Simple
{
    public class ImmutableListModel
    {
        public ImmutableListModel(IReadOnlyList<ImmutableModel> list)
        {
            List = list;
        }

        [SpkXmlArray]
        public IReadOnlyList<ImmutableModel> List { get; }
    }
}
