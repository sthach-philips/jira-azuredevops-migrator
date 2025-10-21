using System.Diagnostics.CodeAnalysis;
using JiraExport;
using NUnit.Framework;

namespace Migration.Jira_Export.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class JiraLinkTests
    {
        [Test]
        public void When_calling_to_string_Then_the_expected_string_value_is_returned()
        {
            var sut = new JiraLink
            {
                LinkType = "System.LinkTypes.Hierarchy-Forward",
                SourceItem = "sourceItem",
                TargetItem = "targetItem"
            };

            var expectedToString = $"[{sut.LinkType}] {sut.SourceItem}->{sut.TargetItem}";

            Assert.That(sut.ToString, Is.EqualTo(expectedToString));
        }

        [Test]
        public void When_calling_equals_with_two_equal_jira_attachments_Then_true_is_returned()
        {
            var sut1 = new JiraLink
            {
                LinkType = "System.LinkTypes.Hierarchy-forward",
                SourceItem = "SourceItem",
                TargetItem = "TargetItem"
            };

            var sut2 = new JiraLink
            {
                LinkType = "System.LinkTypes.Hierarchy-forward",
                SourceItem = "SourceItem",
                TargetItem = "TargetItem"
            };

            Assert.That(() => sut1.Equals(sut2), Is.True);
        }

        [Test]
        public void When_calling_equals_with_null_argumentss_Then_false_is_returned()
        {
            var sut = new JiraLink();
            Assert.That(() => sut.Equals(null), Is.False);
        }
    }
}
