using System.Collections.Generic;
using System.IO;
using System.Text;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.HealthCheck;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.HealthCheck
{
    // The Pages workflow publishes .github/server-notifications.json as v1/notification, and every
    // install fetches it. These tests pin the format so an edit to that file can't break the parse.
    [TestFixture]
    public class ServerSideNotificationServiceFixture : CoreTest<ServerSideNotificationService>
    {
        [SetUp]
        public void Setup()
        {
            Mocker.SetConstant<IReadarrCloudRequestBuilder>(new ReadarrCloudRequestBuilder());
            Mocker.SetConstant<ICacheManager>(new CacheManager());

            Mocker.GetMock<IConfigFileProvider>()
                  .SetupGet(s => s.Branch)
                  .Returns("develop");
        }

        private void GivenResponse(string json)
        {
            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.Execute(It.IsAny<HttpRequest>()))
                  .Returns<HttpRequest>(r => new HttpResponse(r, new HttpHeader(), Encoding.UTF8.GetBytes(json)));
        }

        private static string PublishedFile()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

            while (dir != null && !File.Exists(Path.Combine(dir.FullName, ".github", "server-notifications.json")))
            {
                dir = dir.Parent;
            }

            if (dir == null)
            {
                throw new FileNotFoundException("Could not find .github/server-notifications.json above the test directory");
            }

            return File.ReadAllText(Path.Combine(dir.FullName, ".github", "server-notifications.json"));
        }

        // Parsed directly, because the service swallows parse errors and returns an empty list.
        [Test]
        public void published_notifications_file_should_parse()
        {
            var entries = Json.Deserialize<List<ServerNotificationResponse>>(PublishedFile());

            entries.Should().NotBeNull();
            entries.Should().NotContain(e => string.IsNullOrWhiteSpace(e.Message) || e.Type == HealthCheckResult.Ok);
        }

        [Test]
        public void should_turn_an_entry_into_a_health_check()
        {
            GivenResponse("[{\"type\": \"warning\", \"message\": \"Test message\", \"wikiUrl\": \"https://example.com/docs#anchor\"}]");

            var checks = Subject.GetServerChecks();

            checks.Should().ContainSingle();
            checks[0].Type.Should().Be(HealthCheckResult.Warning);
            checks[0].Message.Should().Be("Test message");
            checks[0].WikiUrl.FullUri.Should().Be("https://example.com/docs#anchor");
        }
    }
}
