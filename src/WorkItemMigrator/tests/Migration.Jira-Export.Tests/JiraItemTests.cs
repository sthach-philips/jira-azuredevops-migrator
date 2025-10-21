using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Web;
using Atlassian.Jira;
using Atlassian.Jira.Remote;
using AutoFixture;
using JiraExport;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;

namespace Migration.Jira_Export.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class JiraItemTests
    {
        private static readonly Fixture _fixture = new();

        [Test]
        public void When_an_attachment_is_added_Then_it_will_be_migrated()
        {
            //Arrange
            var provider = Substitute.For<IJiraProvider>();
            var issueId = _fixture.Create<long>();
            var issueKey = _fixture.Create<string>();
            var attachmentId = _fixture.Create<int>().ToString();
            var attachmentName = _fixture.Create<string>();

            var fields = JObject.Parse($@"{{
                'issuetype': {{ 'name': 'Story' }},
                'attachment': [
                {{
                  'self': 'https://server/rest/api/2/attachment/{attachmentId}',
                  'id': '{attachmentId}',
                  'filename': '{attachmentName}',
                  'author': null,
                  'created': '{DateTime.Now:yyyy - MM - ddTHH:mm: ss.fffZ}',
                  'size': '{_fixture.Create<int>()}',
                  'mimeType': 'image/jpeg',
                  'content': 'https://server/rest/api/2/attachment/{HttpUtility.UrlEncode(attachmentName)}',
                }}
              ]
            }}");
            var renderedFields = JObject.Parse("{ 'custom_field_name': 'SomeValue', 'description': 'RenderedDescription' }");

            var changelog = new List<JObject>() {
                new HistoryItem() // add attachment
                {
                    Field = "Attachment",
                    FieldType = "jira",
                    From = null,
                    FromString = null,
                    To = attachmentId,
                    ToStringValue = attachmentName
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

            //Act
            var jiraItem = JiraItem.CreateFromRest(issueKey, provider);

            //Assert
            Assert.Multiple(() =>
            {
                Assert.AreEqual(2, jiraItem.Revisions.Count);
                Assert.IsFalse(jiraItem.Revisions.All(r => r.AttachmentActions.Count == 0));
                Assert.AreEqual(attachmentName, jiraItem.Revisions[1].AttachmentActions[0].Value.Filename);
                Assert.AreEqual(attachmentId, jiraItem.Revisions[1].AttachmentActions[0].Value.Id);
            });
        }

        [Test]
        public void When_an_attachment_is_added_and_removed_Then_it_cannot_be_migrated_and_is_omitted()
        {
            //Arrange
            var provider = Substitute.For<IJiraProvider>();
            var issueId = _fixture.Create<long>();
            var issueKey = _fixture.Create<string>();
            var attachmentId = _fixture.Create<int>().ToString();
            var attachmentName = _fixture.Create<string>();

            var fields = JObject.Parse($@"{{
                'issuetype': {{ 'name': 'Story' }}
            }}");
            var renderedFields = JObject.Parse("{ 'custom_field_name': 'SomeValue', 'description': 'RenderedDescription' }");

            var changelog = new List<JObject>() {
                new HistoryItem() // add attachment
                {
                    Field = "Attachment",
                    FieldType = "jira",
                    From = null,
                    FromString = null,
                    To = attachmentId,
                    ToStringValue = attachmentName
                }.ToJObject(),
                new HistoryItem() //remove attachment
                {
                    Id = 1,
                    Field = "Attachment",
                    FieldType = "jira",
                    From = attachmentId,
                    FromString = attachmentName,
                    To = null,
                    ToStringValue = null,
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

            //Act
            var jiraItem = JiraItem.CreateFromRest(issueKey, provider);

            //Assert
            Assert.Multiple(() =>
            {
                Assert.AreEqual(3, jiraItem.Revisions.Count);
                Assert.IsTrue(jiraItem.Revisions.All(r => r.AttachmentActions.Count == 0));
            });
        }

        [Test]
        public void When_a_parent_link_is_added_later_Then_it_should_not_be_in_the_initial_revision()
        {
            //Arrange
            var provider = Substitute.For<IJiraProvider>();
            var issueId = _fixture.Create<long>();
            var issueKey = _fixture.Create<string>();
            var parentId = _fixture.Create<long>().ToString();
            const string parentKey = "ISSUE-xx";

            var fields = JObject.Parse($@"{{
                'issuetype': {{ 'name': 'Story' }},
                'parent': {{ 'id': '{parentId}', 'key': '{parentKey}' }}
            }}");
            var renderedFields = JObject.Parse("{ 'custom_field_name': 'SomeValue', 'description': 'RenderedDescription' }");

            var changelog = new List<JObject>() {
                new HistoryItem()
                {
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

            //Act
            var jiraItem = JiraItem.CreateFromRest(issueKey, provider);

            //Assert
            Assert.Multiple(() =>
            {
                Assert.IsFalse(jiraItem.Revisions[0].Fields.ContainsKey("parent"));
                Assert.IsTrue(jiraItem.Revisions[1].Fields.ContainsKey("parent"));
            });
        }

        [Test]
        public void When_a_parent_link_is_changed_later_Then_it_should_not_be_in_the_initial_revision()
        {
            //Arrange
            var provider = Substitute.For<IJiraProvider>();
            var issueId = _fixture.Create<long>();
            var issueKey = _fixture.Create<string>();
            var previousParentId = _fixture.Create<long>().ToString();
            const string previousParentKey = "ISSUE-xx";
            var currentParentId = _fixture.Create<long>().ToString();
            const string currentParentKey = "ISSUE-yy";

            var fields = JObject.Parse($@"{{
                'issuetype': {{ 'name': 'Story' }},
                'parent': {{ 'id': '{currentParentId}', 'key': '{currentParentKey}' }}
            }}");
            var renderedFields = JObject.Parse("{ 'custom_field_name': 'SomeValue', 'description': 'RenderedDescription' }");

            var changelog = new List<JObject>() {
                new HistoryItem()
                {
                    Field = "Parent",
                    FieldType = "jira",
                    From = previousParentId,
                    FromString = previousParentKey,
                    To = currentParentId,
                    ToStringValue = currentParentKey
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

            //Act
            var jiraItem = JiraItem.CreateFromRest(issueKey, provider);

            //Assert
            Assert.Multiple(() =>
            {
                Assert.AreEqual(previousParentKey, jiraItem.Revisions[0].Fields["parent"]);
                Assert.AreEqual(currentParentKey, jiraItem.Revisions[1].Fields["parent"]);
            });
        }

        [Test]
        public void When_a_parent_link_is_added_and_changed_later_Then_it_should_not_be_in_the_initial_revision()
        {
            //Arrange
            var provider = Substitute.For<IJiraProvider>();
            var issueId = _fixture.Create<long>();
            var issueKey = _fixture.Create<string>();
            var previousParentId = _fixture.Create<long>().ToString();
            const string previousParentKey = "PreviousParentKey";
            var currentParentId = _fixture.Create<long>().ToString();
            const string currentParentKey = "CurrentParentKey";

            var fields = JObject.Parse($@"{{
                'issuetype': {{ 'name': 'Story' }}
            }}");
            var renderedFields = JObject.Parse("{ 'custom_field_name': 'SomeValue', 'description': 'RenderedDescription' }");

            var changelog = new List<JObject>() {
                new HistoryItem()
                {
                    Id = 0,
                    Field = "Parent",
                    FieldType = "jira",
                    To = previousParentId,
                    ToStringValue = previousParentKey
                }.ToJObject(),
                new HistoryItem()
                {
                    Id = 1,
                    Field = "Parent",
                    FieldType = "jira",
                    From = previousParentId,
                    FromString = previousParentKey,
                    To = currentParentId,
                    ToStringValue = currentParentKey
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

            //Act
            var jiraItem = JiraItem.CreateFromRest(issueKey, provider);

            //Assert
            Assert.Multiple(() =>
            {
                Assert.IsFalse(jiraItem.Revisions[0].Fields.ContainsKey("parent"));
                Assert.AreEqual(previousParentKey, jiraItem.Revisions[1].Fields["parent"]);
                Assert.AreEqual(currentParentKey, jiraItem.Revisions[2].Fields["parent"]);
            });
        }

        [Test]
        public void When_a_parent_link_was_removed_Then_the_result_should_be_succesful()
        {
            //Arrange
            var provider = Substitute.For<IJiraProvider>();
            var issueId = _fixture.Create<long>();
            var issueKey = _fixture.Create<string>();
            var previousParentId = _fixture.Create<long>().ToString();
            const string previousParentKey = "ISSUE-xx";

            var fields = JObject.Parse($@"{{
                'issuetype': {{ 'name': 'Story' }}
            }}");
            var renderedFields = JObject.Parse("{ 'custom_field_name': 'SomeValue', 'description': 'RenderedDescription' }");

            var changelog = new List<JObject>() {
                new HistoryItem()
                {
                    Field = "Parent",
                    FieldType = "jira",
                    From = previousParentId,
                    FromString = previousParentKey
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

            //Act
            var jiraItem = JiraItem.CreateFromRest(issueKey, provider);

            //Assert
            Assert.Multiple(() =>
            {
                Assert.AreEqual(2, jiraItem.Revisions.Count);
            });
        }

        [Test]
        public void When_an_epic_link_is_added_later_Then_it_should_not_be_in_the_initial_revision()
        {
            //Arrange
            var provider = Substitute.For<IJiraProvider>();
            var issueId = _fixture.Create<long>();
            var issueKey = _fixture.Create<string>();
            var epicId = _fixture.Create<long>().ToString();
            const string epicKey = "EpicKey";

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

            //Act
            var jiraItem = JiraItem.CreateFromRest(issueKey, provider);

            //Assert
            Assert.Multiple(() =>
            {
                Assert.IsFalse(jiraItem.Revisions[0].Fields.ContainsKey(jiraSettings.EpicLinkField));
                Assert.IsTrue(jiraItem.Revisions[1].Fields.ContainsKey(jiraSettings.EpicLinkField));
            });
        }

        [Test]
        public void When_an_epic_link_is_changed_Then_it_should_have_the_previous_value_in_the_initial_revision()
        {
            //Arrange
            var provider = Substitute.For<IJiraProvider>();
            var issueId = _fixture.Create<long>();
            var issueKey = _fixture.Create<string>();
            var currentEpicId = _fixture.Create<long>().ToString();
            const string currentEpicKey = "EpicKey";
            var previousEpicId = _fixture.Create<long>().ToString();
            const string previousEpicKey = "PreviousEpicKey";

            var fields = JObject.Parse(@"{'issuetype': {'name': 'Story'},'EpicLinkField': 'EpicKey'}");
            var renderedFields = JObject.Parse("{ 'custom_field_name': 'SomeValue', 'description': 'RenderedDescription' }");

            var changelog = new List<JObject>() {
                new HistoryItem()
                {
                    Field = "Epic Link",
                    FieldType = "custom",
                    From = previousEpicId,
                    FromString = previousEpicKey,
                    To = currentEpicId,
                    ToStringValue = currentEpicKey
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

            //Act
            var jiraItem = JiraItem.CreateFromRest(issueKey, provider);

            //Assert
            Assert.Multiple(() =>
            {
                Assert.AreEqual(previousEpicKey, jiraItem.Revisions[0].Fields[jiraSettings.EpicLinkField]);
                Assert.AreEqual(currentEpicKey, jiraItem.Revisions[1].Fields[jiraSettings.EpicLinkField]);
            });
        }

        [Test]
        public void When_an_epic_link_is__added_and_changed_later_Then_it_should_not_be_in_the_initial_revision()
        {
            //Arrange
            var provider = Substitute.For<IJiraProvider>();
            var issueId = _fixture.Create<long>();
            var issueKey = _fixture.Create<string>();
            var currentEpicId = _fixture.Create<long>().ToString();
            const string currentEpicKey = "EpicKey";
            var previousEpicId = _fixture.Create<long>().ToString();
            const string previousEpicKey = "PreviousEpicKey";

            var fields = JObject.Parse(@"{'issuetype': {'name': 'Story'},'EpicLinkField': null}");
            var renderedFields = JObject.Parse("{ 'custom_field_name': 'SomeValue', 'description': 'RenderedDescription' }");

            var changelog = new List<JObject>() {
                new HistoryItem()
                {
                    Field = "Epic Link",
                    FieldType = "custom",
                    To = previousEpicId,
                    ToStringValue = previousEpicKey
                }.ToJObject(),
                new HistoryItem()
                {
                    Id = 1,
                    Field = "Epic Link",
                    FieldType = "custom",
                    From = previousEpicId,
                    FromString = previousEpicKey,
                    To = currentEpicId,
                    ToStringValue = currentEpicKey
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

            //Act
            var jiraItem = JiraItem.CreateFromRest(issueKey, provider);

            //Assert
            Assert.Multiple(() =>
            {
                Assert.IsFalse(jiraItem.Revisions[0].Fields.ContainsKey(jiraSettings.EpicLinkField));
                Assert.AreEqual(previousEpicKey, jiraItem.Revisions[1].Fields[jiraSettings.EpicLinkField]);
                Assert.AreEqual(currentEpicKey, jiraItem.Revisions[2].Fields[jiraSettings.EpicLinkField]);
            });
        }

        [Test]
        public void When_an_epic_link_was_removed_Then_the_result_should_be_successful()
        {
            //Arrange
            var provider = Substitute.For<IJiraProvider>();
            var issueId = _fixture.Create<long>();
            var issueKey = _fixture.Create<string>();
            var previousEpicId = _fixture.Create<long>().ToString();
            const string previousEpicKey = "PreviousEpicKey";

            var fields = JObject.Parse(@"{'issuetype': {'name': 'Story'},'EpicLinkField': 'EpicKey'}");
            var renderedFields = JObject.Parse("{ 'custom_field_name': 'SomeValue', 'description': 'RenderedDescription' }");

            var changelog = new List<JObject>() {
                new HistoryItem()
                {
                    Field = "Epic Link",
                    FieldType = "custom",
                    From = previousEpicId,
                    FromString = previousEpicKey
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

            //Act
            var jiraItem = JiraItem.CreateFromRest(issueKey, provider);

            //Assert
            Assert.Multiple(() =>
            {
                Assert.AreEqual(2, jiraItem.Revisions.Count);
            });
        }

        [Test]
        public void When_a_custom_field_is_added_Then_no_customfield_is_added_to_the_revision_with_name_as_key()
        {
            //Arrange
            var provider = Substitute.For<IJiraProvider>();
            var issueId = _fixture.Create<long>();
            var issueKey = _fixture.Create<string>();
            var customFieldId = _fixture.Create<string>();
            var customFieldName = _fixture.Create<string>();

            var fields = JObject.Parse(@"{'issuetype': {'name': 'Story'},'"+customFieldId+@"': {'name':'SomeValue', 'key':'"+customFieldId+"'}}");
            var renderedFields = new JObject();

            var changelog = new List<JObject>();

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

            CustomField customField = null;

            provider.GetCustomField(default).ReturnsForAnyArgs(customField);

            //Act
            var jiraItem = JiraItem.CreateFromRest(issueKey, provider);

            //Assert
            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, jiraItem.Revisions.Count);
                Assert.IsFalse(jiraItem.Revisions[0].Fields.Any(f => f.Key == customFieldName));
            });
        }

        [Test]
        public void When_an_custom_field_is_changed_Then_it_should_have_the_previous_value_in_the_initial_revision()
        {
            //Arrange
            var provider = Substitute.For<IJiraProvider>();
            var issueId = _fixture.Create<long>();
            var issueKey = _fixture.Create<string>();
            var customFieldId = _fixture.Create<string>();
            var customFieldName = _fixture.Create<string>();
            var customFieldPreviousValue = _fixture.Create<string>();
            var customFieldNewValue = _fixture.Create<string>();

            var fields = JObject.Parse(@"{'issuetype': {'name': 'Story'},'"+customFieldId+@"': '"+customFieldNewValue+@"', 'key':'"+issueKey+"'}");
            var renderedFields = new JObject();

            var changelog = new List<JObject>()
            {
                new HistoryItem()
                {
                    Field = customFieldName,
                    FieldType = "custom",
                    From = customFieldPreviousValue,
                    FromString = customFieldPreviousValue,
                    To = customFieldNewValue,
                    ToStringValue = customFieldNewValue
                }.ToJObject()
            };

            var remoteIssue = new JObject
            {
                { "id", issueId },
                { "key", issueKey },
                { "fields", fields },
                { "renderedFields", renderedFields }
            };

            var r = new RemoteField
            {
                id = customFieldId
            };

            var mockedCustomField = new CustomField(r);

            provider.DownloadIssue(default).ReturnsForAnyArgs(remoteIssue);
            provider.DownloadChangelog(default).ReturnsForAnyArgs(changelog);
            var jiraSettings = CreateJiraSettings();
            provider.GetSettings().ReturnsForAnyArgs(jiraSettings);
            provider.GetCustomField(customFieldName).Returns(mockedCustomField);

            //Act
            var jiraItem = JiraItem.CreateFromRest(issueKey, provider);

            //Assert
            Assert.Multiple(() =>
            {
                Assert.AreEqual(2, jiraItem.Revisions.Count);
                Assert.IsTrue(jiraItem.Revisions[0].Fields.ContainsKey(customFieldId));
                Assert.AreEqual(customFieldPreviousValue, jiraItem.Revisions[0].Fields[customFieldId]);
                Assert.IsTrue(jiraItem.Revisions[1].Fields.ContainsKey(customFieldId));
                Assert.AreEqual(customFieldNewValue, jiraItem.Revisions[1].Fields[customFieldId]);
            });
        }

        [Test]
        public void When_an_custom_field_is_added_and_changed_later_Then_it_should_not_be_in_the_initial_revision()
        {
            //Arrange
            var provider = Substitute.For<IJiraProvider>();
            var issueId = _fixture.Create<long>();
            var issueKey = _fixture.Create<string>();
            var customFieldId = _fixture.Create<string>();
            var customFieldName = _fixture.Create<string>();
            var customFieldNewValue = _fixture.Create<string>();

            var fields = JObject.Parse(@"{'issuetype': {'name': 'Story'},'"+customFieldId+@"': '"+customFieldNewValue+@"', 'key':'"+issueKey+"'}");
            var renderedFields = new JObject();

            var changelog = new List<JObject>()
            {
                new HistoryItem()
                {
                    Field = customFieldName,
                    FieldType = "custom",
                    To = customFieldNewValue,
                    ToStringValue = customFieldNewValue
                }.ToJObject()
            };

            var remoteIssue = new JObject
            {
                { "id", issueId },
                { "key", issueKey },
                { "fields", fields },
                { "renderedFields", renderedFields }
            };

            var r = new RemoteField
            {
                id = customFieldId
            };

            var mockedCustomField = new CustomField(r);

            provider.DownloadIssue(default).ReturnsForAnyArgs(remoteIssue);
            provider.DownloadChangelog(default).ReturnsForAnyArgs(changelog);
            var jiraSettings = CreateJiraSettings();
            provider.GetSettings().ReturnsForAnyArgs(jiraSettings);
            provider.GetCustomField(customFieldName).Returns(mockedCustomField);

            //Act
            var jiraItem = JiraItem.CreateFromRest(issueKey, provider);

            //Assert
            Assert.Multiple(() =>
            {
                Assert.AreEqual(2, jiraItem.Revisions.Count);
                Assert.IsFalse(jiraItem.Revisions[0].Fields.ContainsKey(customFieldId));
                Assert.IsFalse(jiraItem.Revisions[0].Fields.ContainsKey(customFieldName));
                Assert.IsTrue(jiraItem.Revisions[1].Fields.ContainsKey(customFieldId));
                Assert.IsFalse(jiraItem.Revisions[1].Fields.ContainsKey(customFieldName));
                Assert.AreEqual(customFieldNewValue, jiraItem.Revisions[1].Fields[customFieldId]);
            });
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