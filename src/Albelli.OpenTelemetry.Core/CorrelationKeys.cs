using JetBrains.Annotations;

namespace Albelli.OpenTelemetry.Core
{
    [PublicAPI]
    public static class CorrelationKeys
    {
        public const string CorrelationId = @"X-CorrelationId";
    }
}