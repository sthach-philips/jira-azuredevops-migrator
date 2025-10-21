using AutoFixture;

using NUnit.Framework;
using System;
using System.Diagnostics.CodeAnalysis;
using WorkItemImport;

namespace Migration.Wi_Import.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class ImportCommandLineTests
    {
        // use auto fixture to help mock and instantiate with dummy data with nsubsitute. 
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _fixture = new Fixture();
        }

        [Test]
        public void When_calling_execute_with_empty_args_Then_an_exception_is_thrown()
        {
            string[] args = null;

            var sut = new ImportCommandLine(args);

            Assert.That(() => sut.Run(), Throws.InstanceOf<NullReferenceException>());
        }

        [Test]
        public void When_calling_execute_with_args_Then_run_is_executed()
        {
            var args = new string[] {
                "--token",
                "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
                "--url",
                "https://dev.azure.com/solidifydemo",
                "--config",
                "C:\\dev\\jira-azuredevops-migrator\\src\\WorkItemMigrator\\Migration.Tests\\test-config-export.json"
            };

            var sut = new ImportCommandLine(args);

            Assert.AreEqual(-1, sut.Run());
        }
    }
}
