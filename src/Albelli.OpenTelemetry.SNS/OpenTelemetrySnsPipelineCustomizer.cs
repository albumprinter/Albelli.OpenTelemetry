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

            //Marshaller handler populates IRequestContext.Request
            pipeline.AddHandlerAfter<Marshaller>(new OpenTelemetrySnsHttpRequestPipelineHandler(_propagator));
            pipeline.AddHandler(new OpenTelemetrySnsMessageAttributePipelineHandler(_propagator));
        }

        public string UniqueName => nameof(OpenTelemetrySnsPipelineCustomizer);
    }
}