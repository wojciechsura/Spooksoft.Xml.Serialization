using Spooksoft.Xml.Serialization.Test.Models.Binary;
using Spooksoft.Xml.Serialization.Test.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Test
{
    [TestClass]
    public class BinarySerializationTests
    {
        [TestMethod]
        public void SimpleSerializeBinaryTest()
        {
            // Arrange

            var model = new BinaryModel
            {
                BinaryData = new byte[] { 0, 255, 1, 128, 52, 99, 115 }
            };

            var serializer = new XmlSerializer();

            // Act

            var deserialized = Automate.SerializeDeserialize(model, serializer);

            // Assert

            Assert.IsNotNull(deserialized);
            Assert.IsNotNull(deserialized.BinaryData);
            Assert.AreEqual(model.BinaryData.Length, deserialized.BinaryData.Length);

            for (int i = 0; i < model.BinaryData.Length; i++)
                Assert.AreEqual(model.BinaryData[i], deserialized.BinaryData[i]);
        }

        [TestMethod]
        public void SerializeNullBinaryTest()
        {
            // Arrange

            var model = new BinaryModel
            {
                BinaryData = null
            };

            var serializer = new XmlSerializer();

            // Act

            var deserialized = Automate.SerializeDeserialize(model, serializer);

            // Assert

            Assert.IsNotNull(deserialized);
            Assert.IsNull(deserialized.BinaryData);            
        }
    }
}
