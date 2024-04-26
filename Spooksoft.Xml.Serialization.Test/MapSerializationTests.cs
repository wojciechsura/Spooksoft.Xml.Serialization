using Spooksoft.Xml.Serialization.Test.Models.Map;
using Spooksoft.Xml.Serialization.Test.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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

        [TestMethod]
        public void ReferenceMapSerializationTest()
        {
            // Arrange

            var model = new ReferenceMapModel
            {
                Dictionary = new()
                {
                    { new DerivedKey { Index = 1 }, new DerivedValue1 { IntValue = 1 } },
                    { new DerivedKey { Index = 2 }, new DerivedValue2 { IntValue = 2 } },
                    { new DerivedKey { Index = 3 }, null }
                }
            };

            var serializer = new XmlSerializer();

            // Act

            var deserialized = Automate.SerializeDeserialize(model, serializer);

            // Assert

            var key1 = new DerivedKey { Index = 1 };
            var key2 = new DerivedKey { Index = 2 };
            var key3 = new DerivedKey { Index = 3 };

            Assert.IsNotNull(deserialized);
            Assert.IsNotNull(deserialized.Dictionary);
            
            Assert.IsTrue(deserialized.Dictionary.ContainsKey(key1));
            Assert.IsNotNull(deserialized.Dictionary[key1]);
            Assert.IsInstanceOfType(deserialized.Dictionary[key1], typeof(DerivedValue1));
            Assert.AreEqual(1, ((DerivedValue1)deserialized.Dictionary[key1]!).IntValue);

            Assert.IsTrue(deserialized.Dictionary.ContainsKey(key2));
            Assert.IsNotNull(deserialized.Dictionary[key2]);
            Assert.IsInstanceOfType(deserialized.Dictionary[key2], typeof(DerivedValue2));
            Assert.AreEqual(2, ((DerivedValue2)deserialized.Dictionary[key2]!).IntValue);

            Assert.IsTrue(deserialized.Dictionary.ContainsKey(key3));
            Assert.IsNull(deserialized.Dictionary[key3]);
        }
    }
}
