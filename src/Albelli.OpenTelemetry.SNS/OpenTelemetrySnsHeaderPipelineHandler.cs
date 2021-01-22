﻿using System.Diagnostics;
using System.Threading.Tasks;
using Albelli.OpenTelemetry.Core;
using Amazon.Runtime;
using Amazon.Runtime.Internal;
using Amazon.SimpleNotificationService.Model;

namespace Albelli.OpenTelemetry.SNS
{
    public class OpenTelemetrySnsHttpRequestPipelineHandler : PipelineHandler
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
            //that piece of code works only *after* Marshaller
            if (!(requestContext?.OriginalRequest is PublishRequest))
            {
                return;
            }

            var request = requestContext.Request;
            var activity = Activity.Current;

            request.TrySetHeader(OpenTelemetryKeys.TraceParent, activity?.Id);
            request.TrySetHeader(OpenTelemetryKeys.TraceState, activity?.TraceStateString);
        }
    }
}