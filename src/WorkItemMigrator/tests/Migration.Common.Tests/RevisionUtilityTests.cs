using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Migration.WIContract;
using NUnit.Framework;

namespace Migration.Common.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class RevisionUtilityTests
    {
        [Test]
        public void When_calling_nextvaliddeltarev_with_one_param_Then_the_expected_result_is_returned()
        {
            var datetime = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

            DateTime expected = datetime + TimeSpan.FromMilliseconds(50);
            DateTime actual = RevisionUtility.NextValidDeltaRev(datetime);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void When_calling_nextvaliddeltarev_with_next_more_than_current_Then_the_expected_result_is_returned()
        {
            var datetime1 = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
            DateTime datetime2 = datetime1 + TimeSpan.FromMilliseconds(60);

            DateTime expected = datetime1 + TimeSpan.FromMilliseconds(50);
            DateTime actual = RevisionUtility.NextValidDeltaRev(datetime1, datetime2);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void When_calling_replacehtmlelements_with_imagewrappattern_Then_the_expected_result_is_returned()
        {
            const string expected = "<img src=\"img.jpg\" />";
            var actual = RevisionUtility.ReplaceHtmlElements("<span class=\"image-wrap\">(<img src=\"img.jpg\" />)</span>");

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void When_calling_replacehtmlelements_with_userlinkpattern_Then_the_expected_result_is_returned()
        {
            const string expected = "<a href=https://text.com class=\"user - hover\" >placeholder string</a>";
            var actual = RevisionUtility.ReplaceHtmlElements("<a href=https://text.com class=\"user - hover\" >placeholder string</a>");

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void When_calling_replacehtmlelements_with_null_parameter_Then_an_exception_is_thrown()
        {
            Assert.Throws<ArgumentNullException>(() => RevisionUtility.ReplaceHtmlElements(null));
        }

        [Test]
        public void When_calling_hasanybyrefname_when_list_is_null_Then_false_is_returned()
        {
            List<WiField> list = null;

            const bool expected = false;
            var actual = RevisionUtility.HasAnyByRefName(list, "name");

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void When_calling_hasanybyrefname_when_list_is_empty_Then_false_is_returned()
        {
            var list = new List<WiField>();

            const bool expected = false;
            var actual = RevisionUtility.HasAnyByRefName(list, "name");

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void When_calling_hasanybyrefname_when_list_contains_matching_refname_Then_true_is_returned()
        {
            var field = new WiField
            {
                ReferenceName = "name"
            };
            var list = new List<WiField>
            {
                field
            };

            const bool expected = true;
            var actual = RevisionUtility.HasAnyByRefName(list, "name");

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void When_calling_hasanybyrefname_when_list_does_not_contain_matching_refname_Then_false_is_returned()
        {
            var field = new WiField
            {
                ReferenceName = "anothername"
            };
            var list = new List<WiField>
            {
                field
            };

            const bool expected = false;
            var actual = RevisionUtility.HasAnyByRefName(list, "name");

            Assert.AreEqual(expected, actual);
        }
    }
}
