using AutoFixture;

using Common.Config;
using JiraExport;
using Migration.Common;
using Migration.Common.Config;
using Migration.WIContract;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Type = Migration.Common.Config.Type;

namespace Migration.Jira_Export.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class JiraMapperTests
    {
        // use auto fixture to help mock and instantiate with dummy data with nsubsitute. 
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _fixture = new Fixture();
        }

        [Test]
        public void When_calling_map_Then_the_expected_result_is_returned()
        {
            JiraItem jiraItem = CreateJiraItem();

            var expectedWiItem = new WiItem
            {
                Type = "User Story",
                OriginId = "issue_key"
            };

            JiraMapper sut = CreateJiraMapper();

            WiItem expected = expectedWiItem;
            WiItem actual = sut.Map(jiraItem);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(expected.OriginId, actual.OriginId);
                Assert.AreEqual(expected.Type, actual.Type);
            });
        }

        [Test]
        public void When_calling_map_with_null_arguments_Then_and_exception_is_thrown()
        {
            JiraMapper sut = CreateJiraMapper();

            Assert.Throws<System.ArgumentNullException>(() => { sut.Map(null); });
        }

        [Test]
        public void When_calling_map_on_an_issue_with_an_epic_link_and_a_parent_Then_two_parent_links_are_mapped()
        {
            //Arrange
            var provider = Substitute.For<IJiraProvider>();
            var issueId = _fixture.Create<long>();
            var issueKey = _fixture.Create<string>();
            var epicId = _fixture.Create<long>().ToString();
            const string epicKey = "EpicKey";
            var parentId = _fixture.Create<long>().ToString();
            const string parentKey = "ParentKey";

            var fields = JObject.Parse(@"{
                'issuetype': {'name': 'Story'},
                'EpicLinkField': 'EpicKey'
            }");
            var renderedFields = JObject.Parse("{ 'custom_field_name': 'SomeValue', 'description': 'RenderedDescription' }");

            var changelog = new List<JObject>() {
                new HistoryItem()
                {
                    Field = "Epic Link",
                    FieldType = "custom",
                    To = epicId,
                    ToStringValue = epicKey
                }.ToJObject(),
                new HistoryItem()
                {
                    Id = 1,
                    Field = "Parent",
                    FieldType = "jira",
                    To = parentId,
                    ToStringValue = parentKey
                }.ToJObject()
            };

            var remoteIssue = new JObject
            {
                { "id", issueId },
                { "key", issueKey },
                { "fields", fields },
                { "renderedFields", renderedFields }
            };

            provider.DownloadIssue(default).ReturnsForAnyArgs(remoteIssue);
            provider.DownloadChangelog(default).ReturnsForAnyArgs(changelog);
            var jiraSettings = CreateJiraSettings();
            provider.GetSettings().ReturnsForAnyArgs(jiraSettings);
            var jiraItem = JiraItem.CreateFromRest(issueKey, provider);
            JiraMapper sut = CreateJiraMapper();

            //Act
            WiItem actual = sut.Map(jiraItem);

            //Assert
            Assert.Multiple(() =>
            {
                Assert.AreEqual(3, actual.Revisions.Count);
                Assert.AreEqual(0, actual.Revisions[0].Links.Count);
                Assert.AreEqual(1, actual.Revisions[1].Links.Count);
                Assert.AreEqual(epicKey, actual.Revisions[1].Links[0].TargetOriginId);
                Assert.AreEqual(1, actual.Revisions[2].Links.Count);
                Assert.AreEqual(parentKey, actual.Revisions[2].Links[0].TargetOriginId);
            });
        }

        [Test]
        public void When_calling_maplinks_Then_the_expected_result_is_returned()
        {
            JiraItem jiraItem = CreateJiraItem();
            var jiraRevision = new JiraRevision(jiraItem);

            JiraMapper sut = CreateJiraMapper();

            var expected = new List<WiLink>();
            List<WiLink> actual = sut.MapLinks(jiraRevision);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void When_calling_maplinks_with_null_arguments_Then_and_exception_is_thrown()
        {
            JiraMapper sut = CreateJiraMapper();

            Assert.Throws<System.ArgumentNullException>(() => { sut.MapLinks(null); });
        }

        [Test]
        public void When_calling_mapattachments_Then_the_expected_result_is_returned()
        {
            JiraItem jiraItem = CreateJiraItem();
            var jiraRevision = new JiraRevision(jiraItem);

            JiraMapper sut = CreateJiraMapper();

            var expected = new List<WiAttachment>();
            List<WiAttachment> actual = sut.MapAttachments(jiraRevision);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void When_calling_mapattachments_with_null_arguments_Then_and_exception_is_thrown()
        {
            JiraMapper sut = CreateJiraMapper();

            Assert.Throws<System.ArgumentNullException>(() => { sut.MapAttachments(null); });
        }

        [Test]
        public void When_calling_mapfields_with_null_arguments_Then_and_exception_is_thrown()
        {
            JiraMapper sut = CreateJiraMapper();

            Assert.Throws<System.ArgumentNullException>(() => { sut.MapFields(null); });
        }

        [Test]
        public void When_calling_mapfields_Then_the_expected_result_is_returned()
        {
            JiraItem jiraItem = CreateJiraItem();
            var jiraRevision = new JiraRevision(jiraItem);
            var expectedWiFieldList = new List<WiField>();

            JiraMapper sut = CreateJiraMapper();

            List<WiField> expected = expectedWiFieldList;
            List<WiField> actual = sut.MapFields(jiraRevision);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void When_calling_truncatefields_with_too_long_title_Then_a_truncated_title_returned()
        {
            const string sourceTitle =
                "test task with max name length - 0123456789012345678901234567890123456789012345"
                + "678901234567890123456789012345678901234567890123456789012345678901234567890123"
                + "456789012345678901234567890123456789012345678901234567890123456789012345678901"
                + "23456789012345678901234567890";
            const string expected =
                "test task with max name length - 0123456789012345678901234567890123456789012345"
                + "678901234567890123456789012345678901234567890123456789012345678901234567890123"
                + "456789012345678901234567890123456789012345678901234567890123456789012345678901"
                + "23456789012345678...";

            JiraMapper sut = CreateJiraMapper();
            var actual = (string)sut.TruncateField(sourceTitle, WiFieldReference.Title);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void When_calling_initializefieldmappings_Then_the_expected_result_is_returned()
        {
            var expectedDictionary = new Dictionary<string, FieldMapping<JiraRevision>>();
            var fieldmap = new FieldMapping<JiraRevision>();
            expectedDictionary.Add("User Story", fieldmap);

            JiraMapper sut = CreateJiraMapper();

            var exportIssuesSummary = new ExportIssuesSummary();

            var expected = expectedDictionary;
            var actual = sut.InitializeFieldMappings(exportIssuesSummary);

            Assert.AreEqual(expected, actual);
        }

        private JiraSettings CreateJiraSettings()
        {
            var settings = new JiraSettings("userID", "pass", "token", "url", "project")
            {
                EpicLinkField = "Epic Link",
                SprintField = "SprintField"
            };

            return settings;
        }

        private JiraMapper CreateJiraMapper()
        {
            var provider = Substitute.For<IJiraProvider>();
            provider.GetSettings().ReturnsForAnyArgs(CreateJiraSettings());

            var cjson = new ConfigJson();

            var f = new FieldMap
            {
                Fields = []
            };
            cjson.FieldMap = f;

            var t = new TypeMap
            {
                Types = []
            };
            var type = new Type
            {
                Source = "Story",
                Target = "User Story"
            };
            t.Types.Add(type);
            cjson.TypeMap = t;

            var linkMap = new LinkMap
            {
                Links = []
            };
            var epicLinkMap = new Link() { Source = "Epic", Target = "System.LinkTypes.Hierarchy-Reverse" };
            var parentLinkMap = new Link() { Source = "Parent", Target = "System.LinkTypes.Hierarchy-Reverse" };
            linkMap.Links.AddRange(new Link[] { epicLinkMap, parentLinkMap });
            cjson.LinkMap = linkMap;

            var repositoryMap = new RepositoryMap
            {
                Repositories = []
            };
            var repository = new Repository
            {
                Source = "Sample Repository",
                Target = "Destination Repository"
            };
            repositoryMap.Repositories.Add(repository);
            cjson.RepositoryMap = repositoryMap;

            var exportIssuesSummary = new ExportIssuesSummary();

            var sut = new JiraMapper(provider, cjson, exportIssuesSummary);

            return sut;
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
            provider.GetSettings().ReturnsForAnyArgs(CreateJiraSettings());

            var jiraItem = JiraItem.CreateFromRest(issueKey, provider);

            return jiraItem;
        }
    }
}
