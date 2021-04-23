using System.Diagnostics;
using Amazon.Runtime.Internal;
using JetBrains.Annotations;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace Albelli.OpenTelemetry.SQS
{
    [PublicAPI]
    public static class OpenTelemetrySqs
    {
        public static readonly ActivitySource Source = new ActivitySource("Albelli.OpenTelemetry.SQS");

        public static void ConfigureAwsOutgoingRequests(TextMapPropagator propagator = null)
        {
            //call to init Sdk static ctor to initialize Propagators.DefaultTextMapPropagator properly
            var _ = Sdk.SuppressInstrumentation;
            propagator ??= Propagators.DefaultTextMapPropagator;
            RuntimePipelineCustomizerRegistry.Instance.Register(new OpenTelemetrySqsPipelineCustomizer(propagator));
        }
    }
}
