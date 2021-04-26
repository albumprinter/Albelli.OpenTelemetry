using System;
using System.Linq;
using Amazon.Runtime.Internal;
using OpenTelemetry.Context.Propagation;

namespace Albelli.OpenTelemetry.SQS
{
    public class OpenTelemetrySqsPipelineCustomizer : IRuntimePipelineCustomizer
    {
        private readonly TextMapPropagator _propagator;

        public OpenTelemetrySqsPipelineCustomizer(TextMapPropagator propagator)
        {
            _propagator = propagator;
        }

        public void Customize(Type type, RuntimePipeline pipeline)
        {
            var handlers = pipeline.Handlers;
            if (handlers.Any(a => a is OpenTelemetrySqsMessageAttributePipelineHandler))
            {
                return;
            }

            //Marshaller handler populates IRequestContext.Request
            pipeline.AddHandlerAfter<Marshaller>(new OpenTelemetrySqsHttpRequestPipelineHandler(_propagator));
            pipeline.AddHandler(new OpenTelemetrySqsMessageAttributePipelineHandler(_propagator));
        }

        public string UniqueName => nameof(OpenTelemetrySqsPipelineCustomizer);
    }
}