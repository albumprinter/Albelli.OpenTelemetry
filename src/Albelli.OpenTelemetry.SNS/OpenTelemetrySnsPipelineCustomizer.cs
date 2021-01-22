using System;
using System.Linq;
using Amazon.Runtime.Internal;

namespace Albelli.OpenTelemetry.SNS
{
    public class OpenTelemetrySnsPipelineCustomizer : IRuntimePipelineCustomizer
    {
        public void Customize(Type type, RuntimePipeline pipeline)
        {
            var handlers = pipeline.Handlers;
            if (handlers.Any(a => a is OpenTelemetrySnsMessageAttributePipelineHandler))
            {
                return;
            }

            //Marshaller handler populates IRequestContext.Request
            pipeline.AddHandlerAfter<Marshaller>(new OpenTelemetrySnsHttpRequestPipelineHandler());
            pipeline.AddHandler(new OpenTelemetrySnsMessageAttributePipelineHandler());
        }

        public string UniqueName => nameof(OpenTelemetrySnsPipelineCustomizer);
    }
}