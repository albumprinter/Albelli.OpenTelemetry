using Albelli.Lambda.Templates.Core.AspNet;
using Albelli.OpenTelemetry.Core;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Albelli.OpenTelemetry.Lambda.SNSEvents
{
    public class OpenTelemetryAspNetRequestPipelineHandler : IAspNetRequestPipelineHandler<SNSEvent.SNSRecord>
    {
        public void PostMarshallRequestFeature(IHttpRequestFeature requestFeature, SNSEvent.SNSRecord snsRecord, ILambdaContext lambdaContext)
        {
            CopyFromMessageToHeaders(requestFeature.Headers, snsRecord.Sns, OpenTelemetryKeys.TraceParent);
            CopyFromMessageToHeaders(requestFeature.Headers, snsRecord.Sns, OpenTelemetryKeys.TraceState);
        }

        private static void CopyFromMessageToHeaders(IHeaderDictionary headerSet, SNSEvent.SNSMessage message, string key)
        {
            var value = message.ExtractAttribute(key);
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            headerSet[key] = value;
        }
    }
}