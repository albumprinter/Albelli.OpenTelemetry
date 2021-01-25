using Albelli.Lambda.Templates.Sns;

namespace Albelli.OpenTelemetry.Lambda.SNSEvents
{
    public static class SnsProxyFunctionExtensions
    {
        public static SnsProxyFunction<TEntity, TStartup> AddOpenTelemetry<TEntity, TStartup>(this SnsProxyFunction<TEntity, TStartup> function)
            where TStartup : class
        {
            function.SnsRecordPipelineHandlers.Add(new OpenTelemetrySnsRecordPipelineHandler());
            function.AspNetRequestPipelineHandlers.Add(new OpenTelemetryAspNetRequestPipelineHandler());

            return function;
        }
    }
}