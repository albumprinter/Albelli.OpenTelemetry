using System.Diagnostics;
using Amazon.Runtime.Internal;
using JetBrains.Annotations;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace Albelli.OpenTelemetry.SNS
{
    [PublicAPI]
    public static class OpenTelemetrySns
    {
        public static readonly ActivitySource Source = new ActivitySource("Albelli.OpenTelemetry.SNS");

        public static void ConfigureAwsOutgoingRequests(TextMapPropagator propagator = null)
        {
            //call to init Sdk static ctor
            var _ = Sdk.SuppressInstrumentation;
            propagator ??= Propagators.DefaultTextMapPropagator;

            RuntimePipelineCustomizerRegistry.Instance.Register(new OpenTelemetrySnsPipelineCustomizer(propagator));
        }
    }
}
