using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using NSubstitute;
using NUnit.Framework;

namespace Migration.Common.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class MigrationCommonTests
    {
        [TestCase("a@jira.com", "a@azdo.com")]
        [TestCase("b@jira.com", "b@azdo.com")]
        public void When_generating_user_map_Then_map_is_correct(string source, string target)
        {
            string[] userMapLines = { "a@jira.com=a@azdo.com", "b@jira.com=b@azdo.com" };
            Dictionary<string, string> generatedUserMap = UserMapper.ParseUserMappings(userMapLines);

            Assert.Multiple(() =>
            {
                Assert.Contains(source, generatedUserMap.Keys);
                Assert.AreEqual(target, generatedUserMap[source]);
            });
        }

        [Test]
        public void When_calling_ParseUserMappings_with_non_exisiting_file_Return_empty_Dictionary()
        {
            //Assign
            var fileSystem = Substitute.For<IFileSystem>();
            fileSystem.File.Exists(Arg.Any<string>()).Returns(true);

            var expected = new Dictionary<string, string>();

            //Act
            var actualResult = UserMapper.ParseUserMappings(string.Empty);

            //Assert
            Assert.That(actualResult.Count, Is.EqualTo(expected.Count));
        }
    }
}
