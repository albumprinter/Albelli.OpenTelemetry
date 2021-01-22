using System.Collections.Generic;
using Amazon.Runtime.Internal;
using Amazon.SQS.Model;
using JetBrains.Annotations;

namespace Albelli.OpenTelemetry.SQS
{
    [PublicAPI]
    public static class OpenTelemetrySqs
    {
        public static void ConfigureAwsOutgoingRequests()
        {
            RuntimePipelineCustomizerRegistry.Instance.Register(new OpenTelemetrySqsPipelineCustomizer());
        }

        internal static void TryAdd(this IRequest request, string key, string value)
        {
            if (request == null || string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            request.Headers[key] = value;
        }

        internal static void TryAdd(this SendMessageRequest sendMessageRequest, string key, string value)
        {
            sendMessageRequest.MessageAttributes ??= new Dictionary<string, MessageAttributeValue>();

            if (!string.IsNullOrWhiteSpace(value) && !sendMessageRequest.MessageAttributes.ContainsKey(key))
            {
                sendMessageRequest.MessageAttributes[key] = new MessageAttributeValue
                {
                    DataType = "String",
                    StringValue = value
                };
            }
        }

        internal static void TryAdd(this SendMessageBatchRequestEntry sendMessageBatchRequestEntry, string key, string value)
        {
            sendMessageBatchRequestEntry.MessageAttributes ??= new Dictionary<string, MessageAttributeValue>();

            if (!string.IsNullOrWhiteSpace(value) && !sendMessageBatchRequestEntry.MessageAttributes.ContainsKey(key))
            {
                sendMessageBatchRequestEntry.MessageAttributes[key] = new MessageAttributeValue
                {
                    DataType = "String",
                    StringValue = value
                };
            }
        }

        internal static void TryAdd(this ReceiveMessageRequest receiveMessageRequest, string key)
        {
            receiveMessageRequest.MessageAttributeNames ??= new List<string>();

            if (!receiveMessageRequest.MessageAttributeNames.Contains(key) &&
                !receiveMessageRequest.MessageAttributeNames.Contains("All"))
            {
                receiveMessageRequest.MessageAttributeNames.Add(key);
            }
        }
    }
}
