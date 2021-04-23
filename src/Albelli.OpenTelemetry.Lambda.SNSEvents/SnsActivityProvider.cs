using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Albelli.OpenTelemetry.Core;
using Amazon.Lambda.SNSEvents;
using JetBrains.Annotations;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace Albelli.OpenTelemetry.Lambda.SNSEvents
{
    [PublicAPI]
    public class SnsActivityProvider
    {
        private readonly TextMapPropagator _propagator;
        public static readonly ActivitySource Source = new ActivitySource("Albelli.OpenTelemetry.SNS");

        public SnsActivityProvider(
            TextMapPropagator propagator
        )
        {
            _propagator = propagator;
        }

        [CanBeNull]
        public Activity Start(SNSEvent.SNSRecord record)
        {
            var activityContext = Activity.Current.SafeGetContext();
            var propagationContext = new PropagationContext(activityContext, Baggage.Current);

            var parentContext = _propagator.Extract(propagationContext, record.Sns.MessageAttributes, ExtractTraceContext);
            Baggage.Current = parentContext.Baggage;

            // https://github.com/open-telemetry/opentelemetry-specification/blob/master/specification/trace/semantic_conventions/messaging.md#span-name
            const string activityName = "SNS receive";
            var activity = Source.StartActivity(activityName, ActivityKind.Consumer, parentContext.ActivityContext);

            activity?.AddTag("messaging.sns.event.source", record.EventSource);
            activity?.AddTag("messaging.sns.event.version", record.EventVersion);
            activity?.AddTag("messaging.sns.event.subscription.arn", record.EventSubscriptionArn);
            activity?.AddTag("messaging.sns.message.id", record.Sns.MessageId);
            activity?.AddTag("messaging.sns.topic.arn", record.Sns.TopicArn);

            return activity;
        }

        private IEnumerable<string> ExtractTraceContext(IDictionary<string, SNSEvent.MessageAttribute> messageAttributes, string key)
        {
            if (messageAttributes == null || !messageAttributes.ContainsKey(key))
            {
                return Enumerable.Empty<string>();
            }

            var messageAttribute = messageAttributes[key];
            return new[] { messageAttribute.Value };
        }
    }
}