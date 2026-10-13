using System;
using FluentAssertions;
using NLog;
using NLog.Layouts;
using NUnit.Framework;
using NzbDrone.Common.Instrumentation;

namespace NzbDrone.Common.Test.InstrumentationTests
{
    [TestFixture]
    public class CleanseLayoutRendererWrapperFixture
    {
        [OneTimeSetUp]
        public void Register()
        {
            LogManager.Setup().SetupExtensions(s => s.RegisterLayoutRenderer<CleanseLayoutRendererWrapper>("cleanse"));
        }

        private static string Render(LogEventInfo logEvent)
        {
            return Layout.FromString(NzbDroneLogger.ConsoleLogLayout).Render(logEvent);
        }

        [Test]
        public void should_mask_secrets_in_the_message()
        {
            var connectionString = "Database=readarr-main;Host=postgres;Username=readarr;Password=hunter2;Port=5432";
            var logEvent = LogEventInfo.Create(LogLevel.Info, "MigrationController", null, "*** Migrating {0} ***", new object[] { connectionString });

            var rendered = Render(logEvent);

            rendered.Should().Be("[Info] MigrationController: *** Migrating Database=readarr-main;Host=postgres;Username=(removed);Password=(removed);Port=5432 *** ");
        }

        [Test]
        public void should_mask_secrets_in_exceptions()
        {
            var logEvent = LogEventInfo.Create(LogLevel.Warn, "HttpClient", new Exception("GET http://indexer/api?t=search&apikey=0123456789abcdef0123456789abcdef failed"), null, "Request failed");

            var rendered = Render(logEvent);

            rendered.Should().StartWith("[Warn] HttpClient: Request failed");
            rendered.Should().Contain("System.Exception: GET http://indexer/api?t=search&apikey=(removed) failed");
        }
    }
}
