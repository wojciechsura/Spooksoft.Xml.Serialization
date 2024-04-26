using Spooksoft.Xml.Serialization.Test.Models.Map;
using Spooksoft.Xml.Serialization.Test.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Test
{
    [TestClass]
    public class MapSerializationTests
    {
        [TestMethod]
        public void SimpleMapSerializationTest()
        {
            // Arrange

            var model = new SimpleMapModel()
            {
                Dictionary = new()
                {
                    { "A", 1 },
                    { "B", 2 }
                }
            };

            var serializer = new XmlSerializer();

            // Act

            var deserialized = Automate.SerializeDeserialize(model, serializer);

            // Assert

            Assert.IsNotNull(deserialized);
            Assert.IsNotNull(deserialized.Dictionary);
            Assert.IsTrue(deserialized.Dictionary.ContainsKey("A"));
            Assert.AreEqual(1, deserialized.Dictionary["A"]);
            Assert.IsTrue(deserialized.Dictionary.ContainsKey("B"));
            Assert.AreEqual(2, deserialized.Dictionary["B"]);
        }

        [TestMethod]
        public void NullMapSerializationTest()
        {
            // Arrange

            var model = new SimpleMapModel()
            {
                Dictionary = null
            };

            var serializer = new XmlSerializer();

            // Act

            var deserialized = Automate.SerializeDeserialize(model, serializer);

            // Assert

            Assert.IsNotNull(deserialized);
            Assert.IsNull(deserialized.Dictionary);            
        }
    }
}
