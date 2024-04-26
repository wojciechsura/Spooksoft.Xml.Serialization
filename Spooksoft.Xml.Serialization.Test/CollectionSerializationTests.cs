using Spooksoft.Xml.Serialization.Test.Models.Collection;
using Spooksoft.Xml.Serialization.Test.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Test
{
    [TestClass]
    public class CollectionSerializationTests
    {
        [TestMethod]
        public void VaryingTypeSerializationTest()
        {
            // Arrange

            var list = new ReferenceListModel
            {
                List = new List<BaseListItem>
                {
                    new ListItemA
                    {
                        IntProperty = 1,
                        StringProperty = "Ala ma kota"
                    },
                    new ListItemB
                    {
                        IntProperty = 2,
                        LongProperty = 3
                    }
                }
            };

            var serializer = new XmlSerializer();

            // Act

            var deserialized = Automate.SerializeDeserialize(list, serializer);

            // Assert

            Assert.IsNotNull(deserialized);
            Assert.IsNotNull(deserialized.List);

            Assert.AreEqual(list.List.Count, deserialized.List.Count);
            
            Assert.IsInstanceOfType(deserialized.List[0], typeof(ListItemA));
            Assert.AreEqual(1, ((ListItemA)deserialized.List[0]).IntProperty);
            Assert.AreEqual("Ala ma kota", ((ListItemA)deserialized.List[0]).StringProperty);

            Assert.IsInstanceOfType(deserialized.List[1], typeof(ListItemB));
            Assert.AreEqual(2, ((ListItemB)deserialized.List[1]).IntProperty);
            Assert.AreEqual(3, ((ListItemB)deserialized.List[1]).LongProperty);
        }

        [TestMethod]
        public void SimpleCollectionSerializationTest()
        {
            // Arrange

            var simpleCollection = new SimpleListModel
            {
                Strings = new List<string?> { "One", "Two" }
            };

            var serializer = new XmlSerializer();

            // Act

            var deserialized = Automate.SerializeDeserialize(simpleCollection, serializer);

            // Assert

            Assert.IsNotNull(deserialized);
            Assert.IsNotNull(deserialized.Strings);
            Assert.AreEqual(2, deserialized.Strings.Count);
            Assert.AreEqual("One", deserialized.Strings[0]);
            Assert.AreEqual("Two", deserialized.Strings[1]);
        }

        [TestMethod]
        public void NullCollectionSerializationTest()
        {
            // Arrange

            var simpleCollection = new SimpleListModel
            {
                Strings = null
            };

            var serializer = new XmlSerializer();

            // Act

            var deserialized = Automate.SerializeDeserialize(simpleCollection, serializer);

            // Assert

            Assert.IsNotNull(deserialized);
            Assert.IsNull(deserialized.Strings);
        }

        [TestMethod]
        public void EmptyCollectionSerializationTest()
        {
            // Arrange

            var simpleCollection = new SimpleListModel
            {
                Strings = new()
            };

            var serializer = new XmlSerializer();

            // Act

            var deserialized = Automate.SerializeDeserialize(simpleCollection, serializer);

            // Assert

            Assert.IsNotNull(deserialized);
            Assert.IsNotNull(deserialized.Strings);
            Assert.AreEqual(0, deserialized.Strings.Count);
        }

        [TestMethod]
        public void NullCollectionItemSerializationTest()
        {
            // Arrange

            var simpleCollection = new SimpleListModel
            {
                Strings = new() { null, null }
            };

            var serializer = new XmlSerializer();

            // Act

            var deserialized = Automate.SerializeDeserialize(simpleCollection, serializer);

            // Assert

            Assert.IsNotNull(deserialized);
            Assert.IsNotNull(deserialized.Strings);
            Assert.AreEqual(2, deserialized.Strings.Count);
            Assert.IsNull(deserialized.Strings[0]);
            Assert.IsNull(deserialized.Strings[1]);
        }

        [TestMethod]
        public void NullArraySerializationTest()
        {
            // Arrange

            var model = new SimpleArrayModel();
            var serializer = new XmlSerializer();

            // Act

            var deserialized = Automate.SerializeDeserialize(model, serializer);

            // Assert

            Assert.IsNotNull(deserialized);
            Assert.IsNull(deserialized.ArrayProp);
        }

        [TestMethod]
        public void SimpleArraySerializationTest()
        {
            // Arrange

            var model = new SimpleArrayModel()
            {
                ArrayProp = new int[] { 3, 12 }
            };
            var serializer = new XmlSerializer();

            // Act

            var deserialized = Automate.SerializeDeserialize(model, serializer);

            // Assert

            Assert.IsNotNull(deserialized);
            Assert.IsNotNull(deserialized.ArrayProp);
            Assert.AreEqual(2, deserialized.ArrayProp.Length);
            Assert.AreEqual(3, deserialized.ArrayProp[0]);
            Assert.AreEqual(12, deserialized.ArrayProp[1]);
        }

        [TestMethod]
        public void ReferenceArraySerializationTest()
        {
            // Arrange

            var model = new ReferenceArrayModel()
            {
                ArrayProp = new BaseListItem?[]
                { 
                    new ListItemA { IntProperty = 1 },
                    null,
                    new ListItemB { IntProperty = 2 }
                }
            };
            var serializer = new XmlSerializer();

            // Act

            var deserialized = Automate.SerializeDeserialize(model, serializer);

            // Assert

            Assert.IsNotNull(deserialized);
            Assert.IsNotNull(deserialized.ArrayProp);
            Assert.AreEqual(3, deserialized.ArrayProp.Length);
            
            Assert.IsNotNull(deserialized.ArrayProp[0]);
            Assert.IsInstanceOfType(deserialized.ArrayProp[0], typeof(ListItemA));
            Assert.AreEqual(1, ((ListItemA)deserialized.ArrayProp[0]!).IntProperty);

            Assert.IsNull(deserialized.ArrayProp[1]);

            Assert.IsNotNull(deserialized.ArrayProp[2]);
            Assert.IsInstanceOfType(deserialized.ArrayProp[2], typeof(ListItemB));
            Assert.AreEqual(2, ((ListItemB)deserialized.ArrayProp[2]!).IntProperty);
        }
    }
}
