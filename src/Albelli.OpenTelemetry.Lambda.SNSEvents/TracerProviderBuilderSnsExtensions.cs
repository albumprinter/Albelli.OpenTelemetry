using OpenTelemetry.Trace;

namespace Albelli.OpenTelemetry.Lambda.SNSEvents
{
    public static class TracerProviderBuilderSnsExtensions
    {
        public static TracerProviderBuilder AddSnsSource(this TracerProviderBuilder tracerProviderBuilder)
        {
            return tracerProviderBuilder.AddSource(OpenTelemetrySnsRecordPipelineHandler.Source.Name);
        }
    }
}