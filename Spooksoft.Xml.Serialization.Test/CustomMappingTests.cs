using Spooksoft.Xml.Serialization.Test.Models.CustomMapping;
using Spooksoft.Xml.Serialization.Test.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Test
{
    [TestClass]
    public class CustomMappingTests
    {
        [TestMethod]
        public void XmlIncludeDerivedMappingMergingTest()
        {
            // Arrange

            var model = new CustomMappingModel
            {
                Property1 = new DerivedPropertyType1(),
                Property2 = new DerivedPropertyType2(),
                Property3 = null,
                List = new List<BasePropertyType?>
                {
                    new DerivedPropertyType1(),
                    new DerivedPropertyType2(),
                    null
                },
                Dictionary = new Dictionary<BasePropertyType, BasePropertyType?>
                {
                    { new DerivedPropertyType1(), new DerivedPropertyType1() },
                    { new DerivedPropertyType2(), new DerivedPropertyType2() },
                    { new BasePropertyType(), null }
                }
            };

            var serializer = new XmlSerializer();

            // Act

            var deserialized = Automate.SerializeDeserialize(model, serializer);

            // Assert

            Assert.IsNotNull(deserialized);

            // Property1

            Assert.IsInstanceOfType(deserialized.Property1, typeof(DerivedPropertyType1));

            // Property2

            Assert.IsInstanceOfType(deserialized.Property2, typeof(DerivedPropertyType2));

            // Property3

            Assert.IsNull(deserialized.Property3);

            // List

            Assert.IsNotNull(deserialized.List);
            Assert.AreEqual(3, deserialized.List.Count);
            Assert.IsInstanceOfType(deserialized.List[0], typeof(DerivedPropertyType1));
            Assert.IsInstanceOfType(deserialized.List[1], typeof(DerivedPropertyType2));
            Assert.IsNull(deserialized.List[2]);

            // Dictionary

            Assert.IsNotNull(deserialized.Dictionary);
            Assert.IsTrue(deserialized.Dictionary.ContainsKey(new DerivedPropertyType1()));
            Assert.IsInstanceOfType(deserialized.Dictionary[new DerivedPropertyType1()], typeof(DerivedPropertyType1));
            Assert.IsTrue(deserialized.Dictionary.ContainsKey(new DerivedPropertyType2()));
            Assert.IsInstanceOfType(deserialized.Dictionary[new DerivedPropertyType2()], typeof(DerivedPropertyType2));
            Assert.IsTrue(deserialized.Dictionary.ContainsKey(new BasePropertyType()));
            Assert.IsNull(deserialized.Dictionary[new BasePropertyType()]);
        }
    }
}
