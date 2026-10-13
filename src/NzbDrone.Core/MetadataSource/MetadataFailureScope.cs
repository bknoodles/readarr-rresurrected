using System;
using System.Threading;

namespace NzbDrone.Core.MetadataSource
{
    // Notes whether a metadata lookup failed while the scope was open. The search methods log an
    // upstream error and return no results, which suits interactive search but makes a failure
    // look the same as "no match". Identification needs to tell the two apart, so it doesn't
    // record a file as searched when the search never got an answer.
    public sealed class MetadataFailureScope : IDisposable
    {
        private static readonly AsyncLocal<MetadataFailureScope> Current = new AsyncLocal<MetadataFailureScope>();

        private readonly MetadataFailureScope _parent;

        private MetadataFailureScope()
        {
            _parent = Current.Value;
            Current.Value = this;
        }

        public bool Failed { get; private set; }

        public static MetadataFailureScope Begin()
        {
            return new MetadataFailureScope();
        }

        // Does nothing outside a scope, so interactive searches are unaffected
        public static void ReportFailure()
        {
            for (var scope = Current.Value; scope != null; scope = scope._parent)
            {
                scope.Failed = true;
            }
        }

        public void Dispose()
        {
            Current.Value = _parent;
        }
    }
}
