using System;
using System.Linq;
using Amazon.Runtime.Internal;

namespace Albelli.OpenTelemetry.SQS
{
    public class OpenTelemetrySqsPipelineCustomizer : IRuntimePipelineCustomizer
    {
        public void Customize(Type type, RuntimePipeline pipeline)
        {
            var handlers = pipeline.Handlers;
            if (handlers.Any(a => a is OpenTelemetrySqsMessageAttributePipelineHandler))
            {
                return;
            }

            //Marshaller handler populates IRequestContext.Request
            pipeline.AddHandlerAfter<Marshaller>(new OpenTelemetrySqsHttpRequestPipelineHandler());
            pipeline.AddHandler(new OpenTelemetrySqsMessageAttributePipelineHandler());
        }

        public string UniqueName => nameof(OpenTelemetrySqsPipelineCustomizer);
    }
}