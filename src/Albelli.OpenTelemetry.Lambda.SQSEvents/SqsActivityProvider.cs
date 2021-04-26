using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Albelli.OpenTelemetry.Core;
using Amazon.Lambda.SQSEvents;
using JetBrains.Annotations;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace Albelli.OpenTelemetry.Lambda.SQSEvents
{
    [PublicAPI]
    public class SqsActivityProvider
    {
        private readonly TextMapPropagator _propagator;
        public static readonly ActivitySource Source = new ActivitySource("Albelli.OpenTelemetry.SQS");

        public SqsActivityProvider(
            TextMapPropagator propagator
        )
        {
            _propagator = propagator;
        }

        [CanBeNull]
        public Activity Start(SQSEvent.SQSMessage message)
        {
            var activityContext = Activity.Current.SafeGetContext();
            var propagationContext = new PropagationContext(activityContext, Baggage.Current);

            var parentContext = _propagator.Extract(propagationContext, message.MessageAttributes, ExtractTraceContext);
            Baggage.Current = parentContext.Baggage;

            // https://github.com/open-telemetry/opentelemetry-specification/blob/master/specification/trace/semantic_conventions/messaging.md#span-name
            const string activityName = "SQS receive";
            var activity = Source.StartActivity(activityName, ActivityKind.Consumer, parentContext.ActivityContext);

            activity?.AddTag("messaging.sqs.event.source", message.EventSource);
            activity?.AddTag("messaging.sqs.event.source.arn", message.EventSourceArn);
            activity?.AddTag("messaging.sqs.message.id", message.MessageId);
            activity?.AddTag("messaging.sqs.aws.region", message.AwsRegion);

            return activity;
        }

        private IEnumerable<string> ExtractTraceContext(IDictionary<string, SQSEvent.MessageAttribute> messageAttributes, string key)
        {
            if (messageAttributes == null || !messageAttributes.ContainsKey(key))
            {
                return Enumerable.Empty<string>();
            }

            var messageAttribute = messageAttributes[key];
            return new[] { messageAttribute.StringValue };
        }
    }
}