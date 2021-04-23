using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Albelli.OpenTelemetry.Core;
using Amazon.Runtime;
using Amazon.Runtime.Internal;
using Amazon.SQS;
using Amazon.SQS.Model;
using JetBrains.Annotations;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace Albelli.OpenTelemetry.SQS
{
    public class OpenTelemetrySqsMessageAttributePipelineHandler : PipelineHandler
    {
        private readonly TextMapPropagator _propagator;

        public OpenTelemetrySqsMessageAttributePipelineHandler(TextMapPropagator propagator)
        {
            _propagator = propagator;
        }

        public override void InvokeSync(IExecutionContext executionContext)
        {
            using var activity = ApplyOpenTelemetry(executionContext);
            base.InvokeSync(executionContext);
        }

        public override async Task<T> InvokeAsync<T>(IExecutionContext executionContext)
        {
            using var activity = ApplyOpenTelemetry(executionContext);
            var result = await base.InvokeAsync<T>(executionContext).ConfigureAwait(false);
            return result;
        }

        private Activity ApplyOpenTelemetry(IExecutionContext executionContext)
        {
            var request = executionContext?.RequestContext?.OriginalRequest;
            //that piece of code works only *before* Marshaller
            if (!(request is AmazonSQSRequest))
            {
                return null;
            }

            // https://github.com/open-telemetry/opentelemetry-specification/blob/master/specification/trace/semantic_conventions/messaging.md#span-name
            var activityName = $"{executionContext.RequestContext.RequestName} send";
            var activity = OpenTelemetrySqs.Source.StartActivity(activityName, ActivityKind.Producer);

            var activityContext = activity.SafeGetContext();
            var propagationContext = new PropagationContext(activityContext, Baggage.Current);

            switch (request)
            {
                case ReceiveMessageRequest receiveMessageRequest:
                    receiveMessageRequest.MessageAttributeNames ??= new List<string>();
                    _propagator.Inject(propagationContext, receiveMessageRequest.MessageAttributeNames, InjectToMessageAttributeNames);

                    break;
                case SendMessageRequest sendMessageRequest:
                    sendMessageRequest.MessageAttributes ??= new Dictionary<string, MessageAttributeValue>();
                    _propagator.Inject(propagationContext, sendMessageRequest.MessageAttributes, InjectToMessageAttributes);

                    break;
                case SendMessageBatchRequest sendMessageBatchRequest:
                    foreach (var sendMessageBatchRequestEntry in sendMessageBatchRequest.Entries)
                    {
                        sendMessageBatchRequestEntry.MessageAttributes ??= new Dictionary<string, MessageAttributeValue>();
                        _propagator.Inject(propagationContext, sendMessageBatchRequestEntry.MessageAttributes, InjectToMessageAttributes);
                    }

                    break;
            }

            AddMessagingTags(activity);
            return activity;
        }

        private static void InjectToMessageAttributeNames(List<string> messageAttributeNames, string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (!messageAttributeNames.Contains(key) &&
                !messageAttributeNames.Contains("All"))
            {
                messageAttributeNames.Add(key);
            }
        }

        private static void InjectToMessageAttributes(Dictionary<string, MessageAttributeValue> messageAttributes, string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (!messageAttributes.ContainsKey(key))
            {
                messageAttributes[key] = new MessageAttributeValue
                {
                    DataType = "String",
                    StringValue = value
                };
            }
        }

        private static void AddMessagingTags([CanBeNull] Activity activity)
        {
            // https://github.com/open-telemetry/opentelemetry-specification/blob/master/specification/trace/semantic_conventions/messaging.md#messaging-attributes
            activity?.SetTag("messaging.system", "AmazonSQS");
            activity?.SetTag("messaging.destination_kind", "queue");
        }
    }
}