using System.Collections.Generic;
using Amazon.Runtime.Internal;
using Amazon.SimpleNotificationService.Model;
using JetBrains.Annotations;

namespace Albelli.OpenTelemetry.SNS
{
    [PublicAPI]
    public static class OpenTelemetrySns
    {
        public static void ConfigureAwsOutgoingRequests()
        {
            RuntimePipelineCustomizerRegistry.Instance.Register(new OpenTelemetrySnsPipelineCustomizer());
        }

        internal static void TrySetHeader(this IRequest request, string key, string value)
        {
            if (request == null || string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            request.Headers[key] = value;
        }

        internal static void TrySetAttribute(this PublishRequest request, string key, string value)
        {
            if (request == null || string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            request.MessageAttributes ??= new Dictionary<string, MessageAttributeValue>();
            request.MessageAttributes[key] = new MessageAttributeValue
            {
                DataType = "String",
                StringValue = value
            };
        }
    }
}
