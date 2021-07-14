using System;
using System.Collections.Generic;
using System.Diagnostics;
using Albelli.OpenTelemetry.Core;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DiagnosticAdapter;

namespace Albelli.OpenTelemetry.AspNetCore
{
    /// <summary>
    /// Diagnostic listener for backward compatibility with the old correlation id format.
    /// Usage:
    /// DiagnosticListener.AllListeners.Subscribe(new AlbelliAspNetCoreCorrelationProxy());
    /// </summary>
    [PublicAPI]
    public sealed class AlbelliAspNetCoreCorrelationProxy : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object>>
    {
        private const string DiagnosticListenerName = "Microsoft.AspNetCore";
        private const string HttpRequestInStart = "Microsoft.AspNetCore.Hosting.HttpRequestIn.Start";
        private const string HttpRequestIn = "Microsoft.AspNetCore.Hosting.HttpRequestIn";
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

        void IObserver<KeyValuePair<string, object>>.OnNext(KeyValuePair<string, object> pair) { }

        void IObserver<KeyValuePair<string, object>>.OnError(Exception error) { }

        void IObserver<KeyValuePair<string, object>>.OnCompleted() { }

        [UsedImplicitly]
        [DiagnosticName(HttpRequestInStart)]
        public void OnHttpRequestInStart(HttpContext httpContext)
        {
            if (httpContext == null) return;

            var currentActivity = Activity.Current;
            var resolvedBackwardsCompatibleId = TryResolveCorrelationId(httpContext, out var guidFromOldSystem);

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

        [DiagnosticName(HttpRequestIn)]
        public void OnHttpRequestIn()
        {
            // Do not do anything with this event. The only reason we are receiving it
            // Is because the listener won't send any other child events if we are not expecting a parent event
            // if (s_diagnosticListener.IsEnabled(DiagnosticsHandlerLoggingStrings.ActivityName, request))
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