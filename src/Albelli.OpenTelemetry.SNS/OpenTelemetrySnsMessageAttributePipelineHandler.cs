using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Albelli.OpenTelemetry.Core;
using Amazon.Runtime;
using Amazon.Runtime.Internal;
using Amazon.SimpleNotificationService.Model;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace Albelli.OpenTelemetry.SNS
{
    public class OpenTelemetrySnsMessageAttributePipelineHandler : PipelineHandler
    {
        private readonly TextMapPropagator _propagator;

        public OpenTelemetrySnsMessageAttributePipelineHandler(TextMapPropagator propagator)
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
            return await base.InvokeAsync<T>(executionContext).ConfigureAwait(false);
        }

        private Activity ApplyOpenTelemetry(IExecutionContext executionContext)
        {
            var awsRequest = executionContext?.RequestContext?.OriginalRequest;
            //that piece of code works only *before* Marshaller
            if (!(awsRequest is PublishRequest request))
            {
                return null;
            }

            // https://github.com/open-telemetry/opentelemetry-specification/blob/master/specification/trace/semantic_conventions/messaging.md#span-name
            const string activityName = "SNS send";
            var activity = OpenTelemetrySns.Source.StartActivity(activityName, ActivityKind.Producer);

            // https://github.com/open-telemetry/opentelemetry-specification/blob/master/specification/trace/semantic_conventions/messaging.md#messaging-attributes
            activity?.SetTag("messaging.system", "AmazonSNS");
            activity?.SetTag("messaging.destination_kind", "queue");

            var activityContext = activity.SafeGetContext();

            request.MessageAttributes ??= new Dictionary<string, MessageAttributeValue>();
            _propagator.Inject(new PropagationContext(activityContext, Baggage.Current), request.MessageAttributes, InjectTraceContext);

            return activity;
        }

        private static void InjectTraceContext(Dictionary<string, MessageAttributeValue> messageAttributes, string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            messageAttributes[key] = new MessageAttributeValue
            {
                DataType = "String",
                StringValue = value
            };
        }
    }
}