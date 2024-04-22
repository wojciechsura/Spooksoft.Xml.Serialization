using Spooksoft.Xml.Serialization.Test.Models.Simple;

namespace Spooksoft.Xml.Serialization.Test
{
    [TestClass]
    public class SimpleSerializationTests
    {
        [TestMethod]
        public void SerializeDeserializeTest()
        {
            // Arrange

            var model = new SimpleModel
            {
                ByteProperty = 1,
                SByteProperty = -1,
                ShortProperty = -2,
                UShortProperty = 2,
                IntProperty = -3,
                UIntProperty = 3,
                LongProperty = -4,
                ULongProperty = 4,
                BoolProperty = true,
                StringProperty = "Ala ma kota",
                FloatProperty = 5.0f,
                DoubleProperty = 6.0,
                DecimalProperty = 7.0m
            };

            var serializer = new XmlSerializer();

            // Act

            var ms = new MemoryStream();
            serializer.Serialize(model, ms);

            ms.Seek(0, SeekOrigin.Begin);
            var deserialized = serializer.Deserialize<SimpleModel>(ms);

            // Assert

            Assert.IsNotNull(deserialized);
            Assert.AreEqual(model.ByteProperty, deserialized.ByteProperty);
            Assert.AreEqual(model.SByteProperty, deserialized.SByteProperty);
            Assert.AreEqual(model.ShortProperty, deserialized.ShortProperty);
            Assert.AreEqual(model.UShortProperty, deserialized.UShortProperty);
            Assert.AreEqual(model.IntProperty, deserialized.IntProperty);
            Assert.AreEqual(model.UIntProperty, deserialized.UIntProperty);
            Assert.AreEqual(model.LongProperty, deserialized.LongProperty);
            Assert.AreEqual(model.ULongProperty, deserialized.ULongProperty);
            Assert.AreEqual(model.BoolProperty, deserialized.BoolProperty);
            Assert.AreEqual(model.StringProperty, deserialized.StringProperty);
            Assert.AreEqual(model.FloatProperty, deserialized.FloatProperty, float.Epsilon);
            Assert.AreEqual(model.DoubleProperty, deserialized.DoubleProperty, double.Epsilon);
            Assert.AreEqual(model.DecimalProperty, deserialized.DecimalProperty);
        }

        [TestMethod]
        public void NestedSerializationTest() 
        {
            // Arrange

            var nested = new NestedModel
            {
                Nested1 = new SimpleModel
                {
                    IntProperty = 42
                },
                Nested2 = null
            };

            var serializer = new XmlSerializer();

            // Act

            var ms = new MemoryStream();
            serializer.Serialize(nested, ms);

            ms.Seek(0, SeekOrigin.Begin);
            var deserialized = serializer.Deserialize<NestedModel>(ms);

            // Assert

            Assert.IsNotNull(deserialized);
            Assert.IsNotNull(deserialized.Nested1);
            Assert.IsNull(deserialized.Nested2);
            Assert.AreEqual(nested.Nested1.IntProperty, deserialized.Nested1.IntProperty);
        }

        [TestMethod]
        public void ImmutableSerializationTest()
        {
            // Arrange

            var immutable = new ImmutableModel(44, "Ala ma kota");

            var serializer = new XmlSerializer();

            // Act

            var ms = new MemoryStream();
            serializer.Serialize(immutable, ms);

            ms.Seek(0, SeekOrigin.Begin);
            var deserialized = serializer.Deserialize<ImmutableModel>(ms);

            // Assert

            Assert.IsNotNull(deserialized);
            Assert.AreEqual(immutable.IntProperty, deserialized.IntProperty);
            Assert.AreEqual(immutable.StringProperty, deserialized.StringProperty);
        }

        [TestMethod]
        public void SimpleListSerializationTest()
        {
            // Arrange

            var list = new SimpleListModel
            {
                ListProperty = new List<SimpleModel>
                {
                    new SimpleModel { IntProperty = 1 },
                    new SimpleModel { IntProperty = 2 }
                }
            };

            var serializer = new XmlSerializer();

            // Act

            var ms = new MemoryStream();
            serializer.Serialize(list, ms);

            ms.Seek(0, SeekOrigin.Begin);
            var deserialized = serializer.Deserialize<SimpleListModel>(ms);

            // Assert

            Assert.IsNotNull(deserialized);
            Assert.IsNotNull(deserialized.ListProperty);
            Assert.AreEqual(list.ListProperty.Count, deserialized.ListProperty.Count);
            Assert.AreEqual(list.ListProperty[0].IntProperty, deserialized.ListProperty[0].IntProperty);
            Assert.AreEqual(list.ListProperty[1].IntProperty, deserialized.ListProperty[1].IntProperty);
        }

        [TestMethod]
        public void ImmutableListSerializationTest()
        {
            // Arrange

            var listValue = new List<ImmutableModel> 
            {
                new(1, "Test1"),
                new(2, "Test2")
            };

            var list = new ImmutableListModel(listValue);
            
            var serializer = new XmlSerializer();

            // Act

            var ms = new MemoryStream();    
            serializer.Serialize(list, ms);

            ms.Seek(0, SeekOrigin.Begin);
            var deserialized = serializer.Deserialize<ImmutableListModel>(ms);

            // Assert

            Assert.IsNotNull(deserialized);
            Assert.IsNotNull(deserialized.List);
            Assert.AreEqual(list.List.Count, deserialized.List.Count);
            Assert.AreEqual(list.List[0].IntProperty, deserialized.List[0].IntProperty);
            Assert.AreEqual(list.List[1].IntProperty, deserialized.List[1].IntProperty);
        }
    }
}