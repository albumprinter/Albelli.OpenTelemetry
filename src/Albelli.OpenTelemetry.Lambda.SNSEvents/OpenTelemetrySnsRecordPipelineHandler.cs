using System.Diagnostics;
using Albelli.Lambda.Templates.Sns;
using Albelli.OpenTelemetry.Core;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using JetBrains.Annotations;

namespace Albelli.OpenTelemetry.Lambda.SNSEvents
{
    public class OpenTelemetrySnsRecordPipelineHandler : ISnsRecordPipelineHandler
    {
        public static readonly ActivitySource Source = new ActivitySource("Albelli.OpenTelemetry.SNS");

        [CanBeNull]
        private Activity _activity = null;

        public void HookBefore(SNSEvent.SNSRecord snsRecord, ILambdaContext lambdaContext)
        {
            var traceParent = snsRecord.Sns.ExtractAttribute(OpenTelemetryKeys.TraceParent);
            var traceState = snsRecord.Sns.ExtractAttribute(OpenTelemetryKeys.TraceState);
            var parentContext = ActivityContext.Parse(traceParent, traceState);

            _activity = Source.StartActivity("SnsRequest", ActivityKind.Consumer, parentContext);

            _activity?.AddTag("sns.event.source", snsRecord.EventSource);
            _activity?.AddTag("sns.event.version", snsRecord.EventVersion);
            _activity?.AddTag("sns.event.subscription.arn", snsRecord.EventSubscriptionArn);
            _activity?.AddTag("sns.message.id", snsRecord.Sns.MessageId);
            _activity?.AddTag("sns.topic.arn", snsRecord.Sns.TopicArn);
        }

        public void HookAfter(SNSEvent.SNSRecord entity, ILambdaContext lambdaContext)
        {
            _activity?.Dispose();
        }
    }
}