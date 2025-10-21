using System.Diagnostics.CodeAnalysis;
using JiraExport;
using NUnit.Framework;

namespace Migration.Jira_Export.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class JiraAttachmentTests
    {
        [Test]
        public void When_calling_to_string_Then_the_expected_string_value_is_returned()
        {
            var sut = new JiraAttachment
            {
                Id = "id",
                Filename = "name"
            };

            var expectedToString = $"{sut.Id}/{sut.Filename}";

            Assert.That(sut.ToString, Is.EqualTo(expectedToString));
        }

        [Test]
        public void When_calling_equals_with_two_equal_jira_attachments_Then_true_is_returned()
        {
            var sut1 = new JiraAttachment();
            var sut2 = new JiraAttachment();

            const string idString = "id";

            sut1.Id = idString;
            sut2.Id = idString;

            Assert.That(() => sut1.Equals(sut2), Is.True);
        }

        [Test]
        public void When_calling_equals_with_null_argumentss_Then_false_is_returned()
        {
            var sut = new JiraAttachment();
            Assert.That(() => sut.Equals(null), Is.False);
        }
    }
}
