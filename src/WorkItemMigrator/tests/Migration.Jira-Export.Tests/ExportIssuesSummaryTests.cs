using System;
using System.Diagnostics.CodeAnalysis;
using JiraExport;
using NUnit.Framework;

namespace Migration.Jira_Export.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class ExportIssuesSummaryTests
    {
        [Test]
        public void When_calling_get_report_string_with_no_unmapped_resources_Then_empty_string_is_returned()
        {
            var sut = new ExportIssuesSummary();

            Assert.That(sut.GetReportString, Is.Empty);
        }

        [Test]
        public void When_calling_get_report_string_with_unmapped_issue_type_Then_the_expected_substring_is_found()
        {
            var issueType = Guid.NewGuid().ToString();
            var sut = new ExportIssuesSummary();

            sut.AddUnmappedIssueType(issueType);

            Assert.That(sut.GetReportString, Contains.Substring($"- {issueType}"));
        }

        [Test]
        public void When_calling_get_report_string_with_unmapped_issue_state_Then_the_expected_substring_is_found()
        {
            var issueType = Guid.NewGuid().ToString();
            var issueState = Guid.NewGuid().ToString();
            var sut = new ExportIssuesSummary();

            sut.AddUnmappedIssueState(issueType, issueState);

            Assert.That(sut.GetReportString, Contains.Substring($"- {issueType}"));
            Assert.That(sut.GetReportString, Contains.Substring($"  - {issueState}"));
        }

        [Test]
        public void When_calling_get_report_string_with_unmapped_user_Then_the_expected_substring_is_found()
        {
            var username = Guid.NewGuid().ToString();
            var sut = new ExportIssuesSummary();

            sut.AddUnmappedUser(username);

            Assert.That(sut.GetReportString, Contains.Substring($"- {username}"));
        }
    }
}
