using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Docs;

namespace NzbDrone.Core.Test.Docs
{
    // Provider "More info" links anchor on display names, so Anchor must produce the same slug
    // GitHub gives the matching heading in docs/supported.md.
    [TestFixture]
    public class DocsUrlsFixture
    {
        [TestCase("Plex Media Server", "plex-media-server")]
        [TestCase("ntfy.sh", "ntfysh")]
        [TestCase("qBittorrent", "qbittorrent")]
        [TestCase("IP Torrents", "ip-torrents")]
        [TestCase("Torrent RSS Feed", "torrent-rss-feed")]
        [TestCase("Docker: bad remote path mapping", "docker-bad-remote-path-mapping")]
        [TestCase("No indexers available with RSS sync enabled, Readarr will not grab new releases automatically", "no-indexers-available-with-rss-sync-enabled-readarr-will-not-grab-new-releases-automatically")]
        public void should_match_githubs_heading_anchors(string heading, string anchor)
        {
            DocsUrls.Anchor(heading).Should().Be(anchor);
        }
    }
}
