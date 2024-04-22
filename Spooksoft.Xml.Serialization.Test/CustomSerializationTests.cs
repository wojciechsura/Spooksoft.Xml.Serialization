using Spooksoft.Xml.Serialization.Test.Models.CustomSerialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Test
{
    [TestClass]
    public class CustomSerializationTests
    {
        [TestMethod]
        public void CustomSerializationTest()
        {
            // Arrange

            var model = new CustomSerializedModel(5, 8);
            var serializer = new XmlSerializer();

            // Act

            var ms = new MemoryStream();
            serializer.Serialize(model, ms);

            ms.Seek(0, SeekOrigin.Begin);
            var deserialized = serializer.Deserialize<CustomSerializedModel>(ms);

            // Assert

            Assert.IsNotNull(deserialized);
            Assert.AreEqual(5, model.IntProperty1);
            Assert.AreEqual(8, model.IntProperty2);
        }
    }
}
