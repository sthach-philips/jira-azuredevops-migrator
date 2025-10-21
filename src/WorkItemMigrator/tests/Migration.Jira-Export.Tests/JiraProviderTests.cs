using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AutoFixture;
using JiraExport;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;
using RestSharp;

namespace Migration.Jira_Export.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class JiraProviderTests
    {
        // use auto fixture to help mock and instantiate with dummy data with nsubsitute. 
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _fixture = new Fixture();
        }

        [Test]
        public void When_calling_getcustomid_Then_the_expected_result_is_returned()
        {
            //Arrange
            var customFieldId = _fixture.Create<string>();
            var propertyName = _fixture.Create<string>();

            var apiResponse = JArray.Parse(
                $"[{{ 'id': 'customfield_00001', 'name': 'Story'}}, " +
                $"{{ 'id': '{customFieldId}', 'name': '{propertyName}'}}]");

            var jiraServiceMock = Substitute.For<IJiraServiceWrapper>();
            jiraServiceMock.RestClient.ExecuteRequestAsync(Method.GET, Arg.Any<string>()).Returns(apiResponse);

            var sut = new JiraProvider(jiraServiceMock);

            var jiraSettings = new JiraSettings("", "", "", "", "")
            {
                JiraApiVersion = 3
            };
            sut.Initialize(jiraSettings, new ExportIssuesSummary());

            //Act
            var id = sut.GetCustomId(propertyName);

            //Assert
            Assert.AreEqual(customFieldId, id);
        }

        [Test]
        public void When_calling_getcustomid_the_key_matches_Then_the_expected_result_is_returned()
        {
            //Arrange
            var customFieldId = _fixture.Create<string>();
            var propertyName = _fixture.Create<string>();

            var apiResponse = JArray.Parse(
                $"[{{ 'id': 'customfield_00001', 'key': 'Story'}}, " +
                $"{{ 'id': '{customFieldId}', 'key': '{propertyName}'}}]");

            var jiraServiceMock = Substitute.For<IJiraServiceWrapper>();
            jiraServiceMock.RestClient.ExecuteRequestAsync(Method.GET, Arg.Any<string>()).Returns(apiResponse);

            var sut = new JiraProvider(jiraServiceMock);

            var jiraSettings = new JiraSettings("", "", "", "", "")
            {
                JiraApiVersion = 3
            };
            sut.Initialize(jiraSettings, new ExportIssuesSummary());

            //Act
            var id = sut.GetCustomId(propertyName);

            //Assert
            Assert.AreEqual(customFieldId, id);
        }

        [Test]
        public void When_calling_getcustomid_and_nothing_matches_Then_null_is_returned()
        {
            //Arrange
            const string propertyName = "does_not_exist";

            var apiResponse = JArray.Parse(
                $"[{{ 'id': 'customfield_00001', 'key': 'Story'}}, " +
                $"{{ 'id': 'customfield_00002', 'key': 'Sprint'}}]");

            var jiraServiceMock = Substitute.For<IJiraServiceWrapper>();
            jiraServiceMock.RestClient.ExecuteRequestAsync(Method.GET, Arg.Any<string>()).Returns(apiResponse);

            var sut = new JiraProvider(jiraServiceMock);

            var jiraSettings = new JiraSettings("", "", "", "", "")
            {
                JiraApiVersion = 3
            };
            sut.Initialize(jiraSettings, new ExportIssuesSummary());

            //Act
            var id = sut.GetCustomId(propertyName);

            //Assert
            Assert.AreEqual(null, id);
        }

        [Test]
        public void When_calling_getcustomid_and_multiple_fields_with_the_same_name_exist_Then_the_first_result_is_returned()
        {
            //Arrange
            const string firstId = "customfield_00001";
            var fieldname = _fixture.Create<string>();

            var apiResponse = JArray.Parse(
                $"[{{ 'id': 'customfield_00001', 'name': '{fieldname}'}}, " +
                $"{{ 'id': 'customfield_00002', 'name': '{fieldname}'}}]");

            var jiraServiceMock = Substitute.For<IJiraServiceWrapper>();
            jiraServiceMock.RestClient.ExecuteRequestAsync(Method.GET, Arg.Any<string>()).Returns(apiResponse);

            var sut = new JiraProvider(jiraServiceMock);

            var jiraSettings = new JiraSettings("", "", "", "", "")
            {
                JiraApiVersion = 3
            };
            sut.Initialize(jiraSettings, new ExportIssuesSummary());

            //Act
            var id = sut.GetCustomId(fieldname);

            //Assert
            Assert.AreEqual(firstId, id);
        }

        [Test]
        public void When_calling_getcustomid_multiple_times_Then_the_api_is_called_once()
        {
            //Arrange
            var customFieldId1 = _fixture.Create<string>();
            var customFieldId2 = _fixture.Create<string>();
            var propertyName1 = _fixture.Create<string>();
            var propertyName2 = _fixture.Create<string>();

            var apiResponse = JArray.Parse(
                $"[{{ 'id': '{customFieldId1}', 'key': '{propertyName1}'}}, " +
                $"{{ 'id': '{customFieldId2}', 'key': '{propertyName2}'}}]");

            var jiraServiceMock = Substitute.For<IJiraServiceWrapper>();
            jiraServiceMock.RestClient.ExecuteRequestAsync(Method.GET, Arg.Any<string>()).Returns(apiResponse);

            var sut = new JiraProvider(jiraServiceMock);

            var jiraSettings = new JiraSettings("", "", "", "", "")
            {
                JiraApiVersion = 3
            };
            sut.Initialize(jiraSettings, new ExportIssuesSummary());

            //Act
            var actualId1 = sut.GetCustomId(propertyName1);
            var actualId2 = sut.GetCustomId(propertyName2);

            //Assert
            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, jiraServiceMock.RestClient.ReceivedCalls().Count());
                Assert.AreEqual(customFieldId1, actualId1);
                Assert.AreEqual(customFieldId2, actualId2);
            });
        }
    }
}