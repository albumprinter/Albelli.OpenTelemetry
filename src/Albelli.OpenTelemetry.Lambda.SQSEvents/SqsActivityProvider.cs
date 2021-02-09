﻿using System.Diagnostics;
using Albelli.OpenTelemetry.Core;
using Amazon.Lambda.SQSEvents;
using JetBrains.Annotations;

namespace Albelli.OpenTelemetry.Lambda.SQSEvents
{
    [PublicAPI]
    public class SqsActivityProvider
    {
        public static readonly ActivitySource Source = new ActivitySource("Albelli.OpenTelemetry.SQS");

        [CanBeNull]
        public Activity Start(SQSEvent.SQSMessage message)
        {
            var traceParent = message.ExtractStringAttribute(OpenTelemetryKeys.TraceParent);
            var traceState = message.ExtractStringAttribute(OpenTelemetryKeys.TraceState);
            var parentContext =
                string.IsNullOrWhiteSpace(traceParent)
                    ? (ActivityContext?)null
                    : ActivityContext.Parse(traceParent, traceState);

            var activity =
                parentContext.HasValue
                    ? Source.StartActivity("SqsRequest", ActivityKind.Consumer, parentContext.Value)
                    : Source.StartActivity("SqsRequest", ActivityKind.Consumer);

            activity?.AddTag("sqs.event.source", message.EventSource);
            activity?.AddTag("sqs.event.source.arn", message.EventSourceArn);
            activity?.AddTag("sqs.message.id", message.MessageId);
            activity?.AddTag("sqs.aws.region", message.AwsRegion);

            return activity;
        }
    }
}