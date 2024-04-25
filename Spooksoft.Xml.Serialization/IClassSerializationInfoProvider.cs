using Spooksoft.Xml.Serialization.Infrastructure;
using Spooksoft.Xml.Serialization.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization
{
    internal interface IClassSerializationInfoProvider
    {
        BaseClassInfo GetClassInfo(Type type);
    }        
}
