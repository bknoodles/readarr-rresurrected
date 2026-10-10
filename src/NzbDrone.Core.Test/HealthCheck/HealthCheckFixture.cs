using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.HealthCheck;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.HealthCheck
{
    [TestFixture]
    public class HealthCheckFixture : CoreTest
    {
        private const string DocsRoot = "https://github.com/ricetim/readarr-rresurrected/blob/develop/docs/";

        [TestCase("I blew up because of some weird user mistake", null, DocsRoot + "health-checks.md#i-blew-up-because-of-some-weird-user-mistake")]
        [TestCase("I blew up because of some weird user mistake", "#my-health-check", DocsRoot + "health-checks.md#my-health-check")]
        [TestCase("I blew up because of some weird user mistake", "custom-page.md#my-health-check", DocsRoot + "custom-page.md#my-health-check")]
        [TestCase("I blew up because of some weird user mistake", "https://example.com/elsewhere#here", "https://example.com/elsewhere#here")]
        public void should_format_wiki_url(string message, string wikiFragment, string expectedUrl)
        {
            var subject = new NzbDrone.Core.HealthCheck.HealthCheck(typeof(HealthCheckBase), HealthCheckResult.Warning, message, wikiFragment);

            subject.WikiUrl.Should().Be(expectedUrl);
        }
    }
}
