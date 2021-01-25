using Amazon.Lambda.SNSEvents;

namespace Albelli.OpenTelemetry.Lambda.SNSEvents
{
    public static class SnsMessageExtensions
    {
        public static string ExtractAttribute(this SNSEvent.SNSMessage snsMessage, string key)
        {
            if (snsMessage?.MessageAttributes != null && snsMessage.MessageAttributes.TryGetValue(key, out var attribute))
            {
                return attribute?.Value;
            }
            return null;
        }
    }
}