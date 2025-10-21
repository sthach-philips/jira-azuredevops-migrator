using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Migration.Common.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class JsonExtensionsTests
    {
        [Test]
        public void When_getvalues_Then_the_expected_result_is_returned()
        {
            var jObject = JObject.Parse(@"{ name: 'My Name', emails: [ 'my@email.com', 'my2@email.com' ]}");
            var expected = jObject.SelectToken("emails", false);
            var actual = jObject.GetValues<JToken>("emails");

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void When_getvalues_with_non_existent_field_Then_an_exception_is_thrown()
        {
            var jObject = JObject.Parse(@"{ name: 'My Name', emails: [ 'my@email.com', 'my2@email.com' ]}");
            Assert.Throws<NullReferenceException>(() => { JsonExtensions.GetValues<JToken>(jObject, "addresses"); });
        }

        [Test]
        public void When_getvalues_with_null_input_Then_an_exception_is_thrown()
        {
            Assert.Throws<ArgumentNullException>(() => { JsonExtensions.GetValues<JToken>(null, ""); });
        }
    }
}
