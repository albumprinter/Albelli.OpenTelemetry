using JetBrains.Annotations;

namespace Albelli.OpenTelemetry.Core
{
    [PublicAPI]
    public class OpenTelemetryKeys
    {
        public const string TraceParent = "traceparent";
        public const string TraceState = "tracestate";
    }
}