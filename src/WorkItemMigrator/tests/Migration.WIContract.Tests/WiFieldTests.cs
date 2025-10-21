using AutoFixture;
using NUnit.Framework;
using System.Diagnostics.CodeAnalysis;

namespace Migration.WIContract.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class WiFieldTests
    {
        // use auto fixture to help instantiate with dummy data
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _fixture = new Fixture();
        }

        [Test]
        public void When_calling_tostring_Then_the_expected_string_value_is_returned()
        {
            var sut = new WiField
            {
                ReferenceName = "referenceName",
                Value = "objValue"
            };

            var expectedToString = $"[{sut.ReferenceName}]={sut.Value}";

            Assert.That(() => sut.ToString(), Is.EqualTo(expectedToString));
        }
    }
}
