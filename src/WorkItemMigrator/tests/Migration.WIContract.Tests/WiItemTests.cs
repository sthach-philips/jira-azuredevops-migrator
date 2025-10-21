using AutoFixture;
using NUnit.Framework;
using System.Diagnostics.CodeAnalysis;

namespace Migration.WIContract.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class WiItemTests
    {
        // use auto fixture to help instantiate with dummy data
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _fixture = new Fixture();
        }

        [Test]
        public void When_calling_ToString_Then_the_expected_String_value_is_returned()
        {
            var sut = new WiItem
            {
                Type = "type",
                OriginId = "originId",
                WiId = 1
            };

            var expectedToString = $"[{sut.Type}]{sut.OriginId}/{sut.WiId}";

            Assert.That(() => sut.ToString(), Is.EqualTo(expectedToString));
        }
    }
}
