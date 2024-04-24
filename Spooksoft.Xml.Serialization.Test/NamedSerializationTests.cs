using NuGet.Frameworks;
using Spooksoft.Xml.Serialization.Test.Models.Named;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Spooksoft.Xml.Serialization.Test
{
    [TestClass]
    public class NamedSerializationTests
    {
        [TestMethod]
        public void SimpleNamedSerializationTest()
        {
            // Arrange

            var model = new SimpleModel
            {
                IntProperty = 23,
                StringProperty = "Ala ma kota"
            };

            var serializer = new XmlSerializer();

            // Act

            var ms = new MemoryStream();
            serializer.Serialize(model, ms);

            ms.Seek(0, SeekOrigin.Begin);
            var doc = new System.Xml.XmlDocument();
            doc.Load(ms);

            // Assert

            Assert.IsNotNull(doc);

            var root = doc.ChildNodes[0];
            Assert.IsNotNull(root);
            Assert.AreEqual("MySimpleModel", root.Name);

            var intPropNode = root.ChildNodes.OfType<XmlElement>().FirstOrDefault(n => n.Name == "MyIntProperty");
            Assert.IsNotNull(intPropNode);
            Assert.AreEqual("23", intPropNode.InnerText);

            Assert.IsNotNull(root.Attributes);
            var stringAttrNode = root.Attributes.OfType<XmlAttribute>().FirstOrDefault(a => a.Name == "MyStringProperty");
            Assert.IsNotNull(stringAttrNode);
            Assert.AreEqual("Ala ma kota", stringAttrNode.Value);
        }

        [TestMethod]
        public void SimpleNamedDeserializationTest()
        {
            // Arrange

            string xml = "<MySimpleModel MyStringProperty=\"Test\"><MyIntProperty>5</MyIntProperty></MySimpleModel>";
            
            var serializer = new XmlSerializer();   
            
            // Act

            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(xml);
            writer.Flush();

            ms.Seek(0, SeekOrigin.Begin);
            var model = serializer.Deserialize<SimpleModel>(ms);

            // Assert

            Assert.IsNotNull(model);
            Assert.AreEqual("Test", model.StringProperty);
            Assert.AreEqual(5, model.IntProperty);
        }
    }
}
