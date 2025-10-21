using System;
using System.Diagnostics.CodeAnalysis;
using JiraExport;
using NUnit.Framework;

namespace Migration.Jira_Export.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class JiraCommandLineTests
    {
        [Test]
        public void When_calling_execute_with_empty_args_Then_an_exception_is_thrown()
        {
            string[] args = null;

            var sut = new JiraCommandLine(args);

            Assert.That(sut.Run, Throws.InstanceOf<NullReferenceException>());
        }

        [Test]
        public void When_calling_execute_with_args_Then_run_is_executed()
        {
            var args = new string[] {
                "-u",
                "john.doe@solidify.dev",
                "-p",
                "XXXXXXXXXXXXXXXXXXXXXXXX",
                "--url",
                "https://solidifydemo.atlassian.net",
                "--config",
                "C:\\dev\\jira-azuredevops-migrator\\src\\WorkItemMigrator\\Migration.Tests\\test-config-export.json"
            };

            var sut = new JiraCommandLine(args);

            Assert.AreEqual(-1, sut.Run());
        }
    }
}
