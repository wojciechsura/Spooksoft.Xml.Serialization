using Spooksoft.Xml.Serialization.Test.Models.CustomSerialization;
using Spooksoft.Xml.Serialization.Test.Utils;
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

            var deserialized = Automate.SerializeDeserialize(model, serializer);

            // Assert

            Assert.IsNotNull(deserialized);
            Assert.AreEqual(5, model.IntProperty1);
            Assert.AreEqual(8, model.IntProperty2);
        }
    }
}
