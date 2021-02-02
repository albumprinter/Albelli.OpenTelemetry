using System;
using System.Collections.Generic;
using System.Diagnostics;
using Albelli.OpenTelemetry.Core;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Albelli.OpenTelemetry.AspNetCore
{
    /// <summary>
    /// Diagnostic listener for backward compatibility with the old correlation id format.
    /// Usage:
    /// DiagnosticListener.AllListeners.Subscribe(new AlbelliAspNetCoreCorrelationProxy());
    /// </summary>
    [PublicAPI]
    public sealed class AlbelliAspNetCoreCorrelationProxy : IObserver<KeyValuePair<string, object>>, IObserver<DiagnosticListener>
    {
        private const string DiagnosticListenerName = "Microsoft.AspNetCore";
        private const string HttpRequestInStart = "Microsoft.AspNetCore.Hosting.HttpRequestIn.Start";
        private readonly ActivitySpanId EMPTY_SPAN = ActivitySpanId.CreateFromString("ffffffffffffffff".AsSpan());
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        public AlbelliAspNetCoreCorrelationProxy()
        {
            // We want to use the W3C format so we can be compatible with the standard as much as possible.
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
            Activity.ForceDefaultIdFormat = true;
        }

        void IObserver<DiagnosticListener>.OnNext(DiagnosticListener diagnosticListener)
        {
            if (diagnosticListener.Name.Equals(DiagnosticListenerName, StringComparison.OrdinalIgnoreCase))
            {
                var subscription = diagnosticListener.SubscribeWithAdapter(this);
                _subscriptions.Add(subscription);
            }
        }

        void IObserver<DiagnosticListener>.OnError(Exception error) { }

        void IObserver<DiagnosticListener>.OnCompleted()
        {
            _subscriptions.ForEach(x => x.Dispose());
            _subscriptions.Clear();
        }

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        private void Start(HttpContext ctx)
        {
            if (ctx == null) return;

            var currentActivity = Activity.Current;
            var resolvedBackwardsCompatibleId = TryResolveCorrelationId(ctx, out var guidFromOldSystem);

            // We can only manipulate the ids if they are in the W3C format
            if (currentActivity != null && resolvedBackwardsCompatibleId)
            {
                var parentId = ActivityTraceId.CreateFromString(guidFromOldSystem.ToString("N").AsSpan());
                if (currentActivity.IdFormat == ActivityIdFormat.W3C)
                {
                    // We want to set the current Activity's parent if:
                    // 1) We don't have an existing parent already -- this means a request came in with no correlation tracking in the new format
                    // 2) If we have a correlation id in the old format that we can set as the current request's parent
                    if (currentActivity.Parent == null)
                    {
                        currentActivity.SetParentId(parentId, EMPTY_SPAN);
                    }
                }
                else if (currentActivity.IdFormat == ActivityIdFormat.Hierarchical)
                {
                    // For whatever reason we received an id that we can't work with,
                    // we will try to override the parent anyway
                    try
                    {
                        currentActivity.SetParentId(parentId, EMPTY_SPAN);
                    }
                    catch
                    {
                        // If it fails, ignore it: there's no way we can restore the correlation
                    }
                }
            }
        }

        public void OnNext(KeyValuePair<string, object> value)
        {
            if (string.Equals(value.Key, HttpRequestInStart, StringComparison.OrdinalIgnoreCase))
            {
                Start(value.Value as HttpContext);
            }
        }

        private bool TryResolveCorrelationId(HttpContext context, out Guid id)
        {
            if (context.Request.Headers.TryGetValue(CorrelationKeys.CorrelationId, out var headers)
                && headers.Count >= 1
                && Guid.TryParse(headers[0], out var correlationId)
                // all zeroes is not a valid argument to ActivityTraceId.CreateFrom where it would be fed
                // so while in the older system it was wrong but technically valid, we have to discard it now
                && correlationId != Guid.Empty)
            {
                id = correlationId;
                return true;
            }

            id = default;
            return false;
        }
    }
}