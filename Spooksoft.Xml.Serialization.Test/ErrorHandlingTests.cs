using Spooksoft.Xml.Serialization.Exceptions;
using Spooksoft.Xml.Serialization.Test.Models.Simple;
using Spooksoft.Xml.Serialization.Test.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spooksoft.Xml.Serialization.Test
{
    [TestClass]
    public class ErrorHandlingTests
    {
        [TestMethod]
        [ExpectedException(typeof(XmlSerializationException))]
        public void MissingCtorParameterTest1()
        {
            // Arrange

            string xml = "<ImmutableModel><StringProperty>5</StringProperty></ImmutableModel>";

            var serializer = new XmlSerializer();

            // Act

            var model = Automate.DeserializeFromString<ImmutableModel>(xml, serializer);
        }

        [TestMethod]
        public void MissingCtorParameterTest2()
        {
            // Arrange

            string xml = "<ImmutableModel><StringProperty>5</StringProperty></ImmutableModel>";

            var config = new XmlSerializerConfig { ReplaceMissingCtorParamsWithDefaultValues = true };
            var serializer = new XmlSerializer(config);

            // Act

            var model = Automate.DeserializeFromString<ImmutableModel>(xml, serializer);

            // Assert

            Assert.IsNotNull(model);
            Assert.AreEqual("5", model.StringProperty);
            Assert.AreEqual(0, model.IntProperty);
        }

        [TestMethod]
        [ExpectedException(typeof(XmlSerializationException))]
        public void InvalidPropertyValueTest1()
        {
            // Arrange

            string xml = "<SimpleModel><IntProperty>Ala ma kota</IntProperty></SimpleModel>";

            var serializer = new XmlSerializer();

            // Act

            var model = Automate.DeserializeFromString<SimpleModel>(xml, serializer);
        }

        [TestMethod]
        public void InvalidPropertyValueTest2()
        {
            // Arrange

            string xml = "<SimpleModel><IntProperty>Ala ma kota</IntProperty></SimpleModel>";

            var config = new XmlSerializerConfig { ErrorOnFailedPropertyDeserialization = false };
            var serializer = new XmlSerializer(config);

            // Act

            var model = Automate.DeserializeFromString<SimpleModel>(xml, serializer);

            // Assert

            Assert.IsNotNull(model);
            Assert.AreEqual(0, model.IntProperty);
        }

    }
}
