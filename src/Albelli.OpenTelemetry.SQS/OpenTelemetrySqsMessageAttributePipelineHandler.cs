using System.Diagnostics;
using System.Threading.Tasks;
using Albelli.OpenTelemetry.Core;
using Amazon.Runtime;
using Amazon.Runtime.Internal;
using Amazon.SQS.Model;

namespace Albelli.OpenTelemetry.SQS
{
    public class OpenTelemetrySqsMessageAttributePipelineHandler : PipelineHandler
    {
        public override void InvokeSync(IExecutionContext executionContext)
        {
            AddTracingDataIfAbsent(executionContext.RequestContext);
            base.InvokeSync(executionContext);
        }

        public override async Task<T> InvokeAsync<T>(IExecutionContext executionContext)
        {
            AddTracingDataIfAbsent(executionContext.RequestContext);
            var result = await base.InvokeAsync<T>(executionContext);
            return result;
        }

        private static void AddTracingDataIfAbsent(IRequestContext requestContext)
        {
            //that piece of code works only *before* Marshaller
            if (!(requestContext.OriginalRequest is Amazon.SQS.AmazonSQSRequest))
            {
                return;
            }

            var currentActivity = Activity.Current;
            switch (requestContext.OriginalRequest)
            {
                case ReceiveMessageRequest receiveMessageRequest:
                    receiveMessageRequest.TryAdd(OpenTelemetryKeys.TraceParent);
                    receiveMessageRequest.TryAdd(OpenTelemetryKeys.TraceState);

                    break;
                case SendMessageRequest sendMessageRequest:
                    sendMessageRequest.TryAdd(OpenTelemetryKeys.TraceParent, currentActivity?.TraceId.ToHexString());
                    sendMessageRequest.TryAdd(OpenTelemetryKeys.TraceState, currentActivity?.TraceStateString);

                    break;
                case SendMessageBatchRequest sendMessageBatchRequest:
                    foreach (var sendMessageBatchRequestEntry in sendMessageBatchRequest.Entries)
                    {
                        sendMessageBatchRequestEntry.TryAdd(OpenTelemetryKeys.TraceParent, currentActivity?.TraceId.ToHexString());
                        sendMessageBatchRequestEntry.TryAdd(OpenTelemetryKeys.TraceState, currentActivity?.TraceStateString);
                    }

                    break;
            }
        }
    }
}