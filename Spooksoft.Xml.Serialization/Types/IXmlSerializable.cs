using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Spooksoft.Xml.Serialization.Types
{
    public interface IXmlSerializable
    {
        void WriteXml(XmlWriter writer);
        void ReadXml(XmlReader reader);
    }
}
