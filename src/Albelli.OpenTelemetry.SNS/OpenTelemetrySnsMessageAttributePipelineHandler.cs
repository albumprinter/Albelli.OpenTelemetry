using System.Diagnostics;
using System.Threading.Tasks;
using Albelli.OpenTelemetry.Core;
using Amazon.Runtime;
using Amazon.Runtime.Internal;
using Amazon.SimpleNotificationService.Model;

namespace Albelli.OpenTelemetry.SNS
{
    public class OpenTelemetrySnsMessageAttributePipelineHandler : PipelineHandler
    {
        public override void InvokeSync(IExecutionContext executionContext)
        {
            AddTracingDataIfAbsent(executionContext.RequestContext);
            base.InvokeSync(executionContext);
        }

        public override async Task<T> InvokeAsync<T>(IExecutionContext executionContext)
        {
            AddTracingDataIfAbsent(executionContext.RequestContext);
            var result = await base.InvokeAsync<T>(executionContext).ConfigureAwait(false);
            return result;
        }

        private static void AddTracingDataIfAbsent(IRequestContext requestContext)
        {
            //that piece of code works only *before* Marshaller
            if (!(requestContext.OriginalRequest is PublishRequest request))
            {
                return;
            }

            var activity = Activity.Current;

            request.TryAdd(OpenTelemetryKeys.TraceParent, activity?.Id);
            request.TryAdd(OpenTelemetryKeys.TraceState, activity?.TraceStateString);
        }
    }
}