using Spooksoft.Xml.Serialization.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Spooksoft.Xml.Serialization.Test.Models.CustomSerialization
{
    public class CustomSerializedModel : IXmlSerializable
    {
        private int field1;
        private int field2;

        public CustomSerializedModel()
        {

        }

        public CustomSerializedModel(int field1, int field2)
        {
            this.field1 = field1;
            this.field2 = field2;
        }

        public void Read(XmlElement element)
        {
            string? values = element.GetAttribute("Values");

            if (values != null)
            {
                var splitted = values.Split('|');
                if (splitted.Length == 2)
                {
                    int value = 0;
                    if (int.TryParse(splitted[0], out value))
                        field1 = value;
                    if (int.TryParse(splitted[1], out value))
                        field2 = value;
                }
            }
        }

        public void Write(XmlElement element)
        {
            element.SetAttribute("Values", $"{IntProperty1}|{IntProperty2}");
        }

        public int IntProperty1 => field1;
        public int IntProperty2 => field2;
    }
}
