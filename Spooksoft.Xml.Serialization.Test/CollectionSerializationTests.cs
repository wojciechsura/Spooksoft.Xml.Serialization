using Spooksoft.Xml.Serialization.Test.Models.Collection;
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

            var list = new CollectionModel
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

            var ms = new MemoryStream();
            serializer.Serialize(list, ms);

            ms.Seek(0, SeekOrigin.Begin);
            var deserialized = serializer.Deserialize<CollectionModel>(ms);

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
    }
}
