using NLog.LayoutRenderers;
using NLog.LayoutRenderers.Wrappers;

namespace NzbDrone.Common.Instrumentation
{
    // ${cleanse:inner=...} masks API keys, passwords and the like, as NzbDroneFileTarget does for
    // the log files. The console needs it too: in Docker it is what `docker logs` shows.
    [LayoutRenderer("cleanse")]
    public sealed class CleanseLayoutRendererWrapper : WrapperLayoutRendererBase
    {
        protected override string Transform(string text)
        {
            return CleanseLogMessage.Cleanse(text);
        }
    }
}
