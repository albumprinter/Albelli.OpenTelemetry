using System;
using System.Linq;
using Amazon.Runtime.Internal;
using OpenTelemetry.Context.Propagation;

namespace Albelli.OpenTelemetry.SNS
{
    public class OpenTelemetrySnsPipelineCustomizer : IRuntimePipelineCustomizer
    {
        private readonly TextMapPropagator _propagator;

        public OpenTelemetrySnsPipelineCustomizer(TextMapPropagator propagator)
        {
            _propagator = propagator;
        }

        public void Customize(Type type, RuntimePipeline pipeline)
        {
            var handlers = pipeline.Handlers;
            if (handlers.Any(a => a is OpenTelemetrySnsMessageAttributePipelineHandler))
            {
                return;
            }

            //traceparent, tracestate excluded from signed headers
            pipeline.AddHandlerAfter<Signer>(new OpenTelemetrySnsHttpRequestPipelineHandler(_propagator));
            pipeline.AddHandlerAfter<Signer>(new OpenTelemetrySnsMessageAttributePipelineHandler(_propagator));
        }

        public string UniqueName => nameof(OpenTelemetrySnsPipelineCustomizer);
    }
}