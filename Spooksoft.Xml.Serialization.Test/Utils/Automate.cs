using Spooksoft.Xml.Serialization.Test.Models.Simple;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Test.Utils
{
    internal static class Automate
    {
        public static T? SerializeDeserialize<T>(T? item, XmlSerializer serializer)
            where T : class
        {
            var ms = new MemoryStream();
            serializer.Serialize(item, ms);

            // DEBUG START
            ms.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(ms);
            string xml = reader.ReadToEnd();
            System.Diagnostics.Debug.WriteLine(xml);
            // DEBUG END

            ms.Seek(0, SeekOrigin.Begin);
            var deserialized = serializer.Deserialize<T>(ms);
            
            return deserialized;
        }

        public static T? DeserializeFromString<T>(string xml, XmlSerializer serializer)
            where T : class
        {
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(xml);
            writer.Flush();

            ms.Seek(0, SeekOrigin.Begin);
            var result = serializer.Deserialize<T>(ms);

            return result;
        }
    }
}
