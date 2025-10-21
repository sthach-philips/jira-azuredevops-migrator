using System.Diagnostics.CodeAnalysis;
using JiraExport;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;

namespace Migration.Jira_Export.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class JiraRevisionTests
    {
        [Test]
        public void When_calling_compare_to_with_null_argumentss_Then_1_is_returned()
        {
            var sut1 = new JiraRevision(CreateJiraItem());
            Assert.That(() => sut1.CompareTo(null), Is.EqualTo(1));
        }

        [Test]
        public void When_calling_compare_to_with_equal_objects_Then_0_is_returned()
        {
            var sut1 = new JiraRevision(CreateJiraItem());
            var sut2 = new JiraRevision(CreateJiraItem());

            Assert.That(() => sut1.CompareTo(sut2), Is.EqualTo(0));
        }

        [Test]
        public void When_calling_compare_to_with_non_equal_objects_Then_1_is_returned()
        {
            var sut1 = new JiraRevision(CreateJiraItem());
            var sut2 = new JiraRevision(CreateJiraItem());
            sut1.Time = System.DateTime.Now;

            Assert.That(() => sut1.CompareTo(sut2), Is.EqualTo(1));
        }

        private JiraItem CreateJiraItem()
        {
            var provider = Substitute.For<IJiraProvider>();

            var issueType = JObject.Parse(@"{ 'issuetype': {'name': 'Story'}}");
            var renderedFields = JObject.Parse("{ 'custom_field_name': 'SomeValue', 'description': 'RenderedDescription' }");
            const string issueKey = "issue_key";

            var remoteIssue = new JObject
            {
                { "fields", issueType },
                { "renderedFields", renderedFields },
                { "key", issueKey }
            };

            provider.DownloadIssue(default).ReturnsForAnyArgs(remoteIssue);
            provider.DownloadChangelog(default).ReturnsForAnyArgs(new System.Collections.Generic.List<JObject>());
            provider.GetSettings().ReturnsForAnyArgs(CreateJiraSettings());

            var jiraItem = JiraItem.CreateFromRest(issueKey, provider);

            return jiraItem;
        }

        private JiraSettings CreateJiraSettings()
        {
            var settings = new JiraSettings("userID", "pass", "token", "url", "project")
            {
                EpicLinkField = "EpicLinkField",
                SprintField = "SprintField"
            };

            return settings;
        }
    }
}
