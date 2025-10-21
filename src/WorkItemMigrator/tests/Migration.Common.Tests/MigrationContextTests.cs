using System.Diagnostics.CodeAnalysis;
using System.IO;
using Common.Config;
using NUnit.Framework;

namespace Migration.Common.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class MigrationContextTests
    {
        [Test]
        public void When_initializing_migration_context_Then_folder_paths_are_correct()
        {
            var config = new ConfigJson
            {
                AttachmentsFolder = "AttachmentsFolder",
                UserMappingFile = "UserMappingFile",
                Workspace = "C:\\Temp\\JiraExport\\"
            };
            MigrationContext.Init("app", config, "debug", true, "");

            Assert.Multiple(() =>
            {
                Assert.That(MigrationContext.Instance.AttachmentsPath, Is.EqualTo(Path.Combine(config.Workspace, config.AttachmentsFolder)));
                Assert.That(MigrationContext.Instance.UserMappingPath, Is.EqualTo(Path.Combine(config.Workspace, config.UserMappingFile)));
            });
        }
    }
}
