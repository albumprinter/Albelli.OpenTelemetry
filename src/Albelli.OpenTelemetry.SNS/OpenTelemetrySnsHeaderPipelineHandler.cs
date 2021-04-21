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
    public class OpenTelemetrySnsHttpRequestPipelineHandler : PipelineHandler
    {
        private readonly TextMapPropagator _propagator;

        public OpenTelemetrySnsHttpRequestPipelineHandler(TextMapPropagator propagator)
        {
            _propagator = propagator;
        }

        public override void InvokeSync(IExecutionContext executionContext)
        {
            ApplyOpenTelemetry(executionContext.RequestContext);
            base.InvokeSync(executionContext);
        }

        public override async Task<T> InvokeAsync<T>(IExecutionContext executionContext)
        {
            ApplyOpenTelemetry(executionContext.RequestContext);
            return await base.InvokeAsync<T>(executionContext).ConfigureAwait(false);
        }

        private void ApplyOpenTelemetry(IRequestContext requestContext)
        {
            //that piece of code works only *after* Marshaller
            if (!(requestContext?.OriginalRequest is PublishRequest))
            {
                return;
            }

            var request = requestContext.Request;
            var activity = Activity.Current;

            var activityContext = activity.SafeGetContext();
            _propagator.Inject(new PropagationContext(activityContext, Baggage.Current), request.Headers, InjectTraceContext);
        }

        private static void InjectTraceContext(IDictionary<string, string> headers, string key, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            headers[key] = value;
        }
    }
}