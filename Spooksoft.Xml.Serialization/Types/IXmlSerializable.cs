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
        void Write(XmlElement element);
        void Read(XmlElement element);
    }
}
