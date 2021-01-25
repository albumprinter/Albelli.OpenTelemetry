using System.Diagnostics;
using Albelli.OpenTelemetry.Core;
using Amazon.Lambda.SNSEvents;
using JetBrains.Annotations;

namespace Albelli.OpenTelemetry.Lambda.SNSEvents
{
    public class SnsActivityProvider
    {
        public static readonly ActivitySource Source = new ActivitySource("Albelli.OpenTelemetry.SNS");

        [CanBeNull]
        public Activity Start(SNSEvent.SNSRecord record)
        {
            var traceParent = record.Sns.ExtractAttribute(OpenTelemetryKeys.TraceParent);
            var traceState = record.Sns.ExtractAttribute(OpenTelemetryKeys.TraceState);
            var parentContext = ActivityContext.Parse(traceParent, traceState);

            var activity = Source.StartActivity("SnsRequest", ActivityKind.Consumer, parentContext);

            activity?.AddTag("sns.event.source", record.EventSource);
            activity?.AddTag("sns.event.version", record.EventVersion);
            activity?.AddTag("sns.event.subscription.arn", record.EventSubscriptionArn);
            activity?.AddTag("sns.message.id", record.Sns.MessageId);
            activity?.AddTag("sns.topic.arn", record.Sns.TopicArn);

            return activity;
        }
    }
}