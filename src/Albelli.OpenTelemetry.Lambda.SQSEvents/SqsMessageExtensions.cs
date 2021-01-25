using Amazon.Lambda.SQSEvents;

namespace Albelli.OpenTelemetry.Lambda.SQSEvents
{
    public static class SqsMessageExtensions
    {
        public static string ExtractStringAttribute(this SQSEvent.SQSMessage message, string key)
        {
            if (message?.MessageAttributes != null && message.MessageAttributes.TryGetValue(key, out var attribute))
            {
                return attribute?.StringValue;
            }
            return null;
        }
    }
}