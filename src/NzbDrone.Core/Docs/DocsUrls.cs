using System.Text.RegularExpressions;

namespace NzbDrone.Core.Docs
{
    // Links into this project's documentation in docs/, as GitHub renders it. Anchors are GitHub's
    // heading slugs. .github/scripts/check_docs_anchors.py checks that every anchor the code links
    // to exists, so run it after renaming a heading, a health check fragment or a provider.
    public static class DocsUrls
    {
        public const string Base = "https://github.com/ricetim/readarr-rresurrected/blob/develop/docs/";
        public const string HealthChecks = Base + "health-checks.md";
        public const string Settings = Base + "settings.md";
        public const string Supported = Base + "supported.md";
        public const string Troubleshooting = Base + "troubleshooting.md";

        private static readonly Regex NonAnchorCharacters = new Regex(@"[^\p{L}\p{N} _-]", RegexOptions.Compiled);

        // The anchor GitHub gives a heading: lower case, punctuation dropped, spaces as hyphens.
        public static string Anchor(string heading)
        {
            return NonAnchorCharacters.Replace(heading.Trim().ToLowerInvariant(), string.Empty).Replace(' ', '-');
        }
    }
}
