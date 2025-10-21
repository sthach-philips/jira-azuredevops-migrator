using AutoFixture;

using Common.Config;
using JiraExport;
using Migration.Common.Config;
using Migration.WIContract;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Migration.Jira_Export.Tests.RevisionUtils
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class LinkMapperUtilsTests
    {
        private JiraRevision MockRevisionWithParentItem(string issueKey, string revisionSummary)
        {
            var provider = Substitute.For<IJiraProvider>();

            var remoteIssue = new JObject
            {
                { "fields", new JObject() },
                { "renderedFields", new JObject() },
                { "key", issueKey }
            };

            provider.DownloadIssue(default).ReturnsForAnyArgs(remoteIssue);
            var settings = new JiraSettings("userID", "pass", "token", "url", "project")
            {
                SprintField = "SprintField"
            };
            provider.GetSettings().ReturnsForAnyArgs(settings);

            var item = JiraItem.CreateFromRest(issueKey, provider);

            var revision = new JiraRevision(item)
            {
                Fields = new Dictionary<string, object>
                {
                    ["summary"] = revisionSummary
                }
            };

            return revision;
        }

        //public static void AddSingleLink(JiraRevision r, List<WiLink> links, string field, string type, ConfigJson config)

        [Test]
        public void When_calling_add_single_link_with_empty_string_arg_Then_an_exception_is_thrown()
        {
            var revision = MockRevisionWithParentItem("test-key", "test-summary");

            Assert.That(() => LinkMapperUtils.AddSingleLink(revision, [], "", "", new ConfigJson()), Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void When_calling_add_single_link_with_valid_field_Then_a_link_is_added()
        {
            const string issueKey = "issue_key";
            const string summary = "My Summary";
            const string targetId = "Target_ID";
            const string targetWiType = "Target_Wi_Type";
            const string child = "Child";
            const string epicChild = "epic child";

            JiraRevision revision = MockRevisionWithParentItem(issueKey, summary);

            var link = new Link
            {
                Source = child,
                Target = targetWiType
            };

            var configJson = new ConfigJson
            {
                LinkMap = new LinkMap
                {
                    Links =
                    [
                        link
                    ]
                }
            };

            var links = new List<WiLink>();

            revision.Fields[epicChild] = targetId;

            LinkMapperUtils.AddSingleLink(revision, links, epicChild, child, configJson);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(ReferenceChangeType.Added, links[0].Change);
                Assert.AreEqual(issueKey, links[0].SourceOriginId);
                Assert.AreEqual(targetId, links[0].TargetOriginId);
                Assert.AreEqual(targetWiType, links[0].WiType);
            });

        }

        [Test]
        public void When_calling_add_single_link_with_null_field_Then_no_link_is_added()
        {
            const string issueKey = "issue_key";
            const string summary = "My Summary";
            const string targetWiType = "Target_Wi_Type";
            const string child = "Child";
            const string epicChild = "epic child";

            JiraRevision revision = MockRevisionWithParentItem(issueKey, summary);

            var link = new Link
            {
                Source = child,
                Target = targetWiType
            };

            var configJson = new ConfigJson
            {
                LinkMap = new LinkMap
                {
                    Links =
                    [
                        link
                    ]
                }
            };

            var links = new List<WiLink>();

            revision.Fields[epicChild] = null;

            LinkMapperUtils.AddSingleLink(revision, links, epicChild, child, configJson);

            Assert.IsEmpty(links);
        }

        [Test]
        public void When_calling_add_single_link_with_null_arguments_Then_and_exception_is_thrown()
        {
            Assert.Throws<ArgumentNullException>(() => { LinkMapperUtils.AddSingleLink(null, null, null, null, null); });
        }

        [Test]
        public void When_calling_add_remove_single_link_with_empty_string_arg_Then_an_exception_is_thrown()
        {
            var revision = MockRevisionWithParentItem("test-key", "test-summary");

            Assert.That(() => LinkMapperUtils.AddRemoveSingleLink(revision, [], "", "", new ConfigJson()), Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void When_calling_add_remove_single_link_with_valid_field_Then_a_link_is_added()
        {
            const string issueKey = "issue_key";
            const string summary = "My Summary";
            const string targetId = "Target_ID";
            const string targetWiType = "Target_Wi_Type";
            const string child = "Child";
            const string epicChild = "epic child";

            JiraRevision revision = MockRevisionWithParentItem(issueKey, summary);

            var link = new Link
            {
                Source = child,
                Target = targetWiType
            };

            var configJson = new ConfigJson
            {
                LinkMap = new LinkMap
                {
                    Links =
                    [
                        link
                    ]
                }
            };

            var links = new List<WiLink>();

            revision.Fields[epicChild] = targetId;

            LinkMapperUtils.AddRemoveSingleLink(revision, links, epicChild, child, configJson);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(ReferenceChangeType.Added, links[0].Change);
                Assert.AreEqual(issueKey, links[0].SourceOriginId);
                Assert.AreEqual(targetId, links[0].TargetOriginId);
                Assert.AreEqual(targetWiType, links[0].WiType);
            });

        }

        [Test]
        public void When_calling_add_remove_single_link_with_valid_field_Then_a_link_is_removed()
        {
            const string issueKey = "issue_key";
            const string summary = "My Summary";
            const string targetId = "Target_ID";
            const string targetWiType = "Target_Wi_Type";
            const string child = "Child";
            const string epicChild = "epic child";

            JiraRevision revision = MockRevisionWithParentItem(issueKey, summary);
            JiraRevision revision2 = MockRevisionWithParentItem(issueKey, summary);
            JiraRevision revision3 = MockRevisionWithParentItem(issueKey, summary);

            revision.Index = 1;
            revision.Fields[epicChild] = null;
            revision2.Fields[epicChild] = targetId;
            revision3.Fields[epicChild] = targetId;
            revision.ParentItem.Revisions.Insert(0, revision2);
            revision2.ParentItem.Revisions.Insert(0, revision);
            revision.ParentItem.Revisions.Insert(0, revision);

            var link = new Link
            {
                Source = child,
                Target = targetWiType
            };

            var configJson = new ConfigJson
            {
                LinkMap = new LinkMap
                {
                    Links =
                    [
                        link
                    ]
                }
            };

            var links = new List<WiLink>();

            LinkMapperUtils.AddRemoveSingleLink(revision, links, epicChild, child, configJson);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(ReferenceChangeType.Removed, links[0].Change);
                Assert.AreEqual(issueKey, links[0].SourceOriginId);
                Assert.AreEqual(targetId, links[0].TargetOriginId);
                Assert.AreEqual(targetWiType, links[0].WiType);
            });
        }

        [Test]
        public void When_calling_add_remove_single_link_with_null_arguments_Then_and_exception_is_thrown()
        {
            Assert.Throws<ArgumentNullException>(() => { LinkMapperUtils.AddRemoveSingleLink(null, null, null, null, null); });
        }

        [Test]
        public void When_calling_map_epic_child_link_with_empty_string_arg_Then_an_exception_is_thrown()
        {
            var revision = MockRevisionWithParentItem("test-key", "test-summary");

            Assert.That(() => LinkMapperUtils.MapEpicChildLink(revision, [], "", "", new ConfigJson()), Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void When_calling_map_epic_child_link_with_valid_field_Then_a_link_is_added()
        {
            // issueKey must be > targetId for a link to be generated
            const string issueKey = "9";
            const string targetId = "8";
            const string summary = "My Summary";
            const string targetWiType = "Target_Wi_Type";
            const string child = "Child";
            const string epicChild = "epic child";

            JiraRevision revision = MockRevisionWithParentItem(issueKey, summary);

            var link = new Link
            {
                Source = child,
                Target = targetWiType
            };

            var configJson = new ConfigJson
            {
                LinkMap = new LinkMap
                {
                    Links =
                    [
                        link
                    ]
                }
            };

            var links = new List<WiLink>();

            revision.Fields[epicChild] = targetId;

            LinkMapperUtils.MapEpicChildLink(revision, links, epicChild, child, configJson);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(ReferenceChangeType.Added, links[0].Change);
                Assert.AreEqual(issueKey, links[0].SourceOriginId);
                Assert.AreEqual(targetId, links[0].TargetOriginId);
                Assert.AreEqual(targetWiType, links[0].WiType);
            });
        }

        [Test]
        public void When_calling_map_epic_child_link_with_null_arguments_Then_and_exception_is_thrown()
        {
            Assert.Throws<ArgumentNullException>(() => { LinkMapperUtils.MapEpicChildLink(null, null, null, null, null); });
        }
    }
}
