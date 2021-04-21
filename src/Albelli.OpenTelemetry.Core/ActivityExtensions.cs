using System.Diagnostics;
using JetBrains.Annotations;

namespace Albelli.OpenTelemetry.Core
{
    public static class ActivityExtensions
    {
        /// <summary>
        /// Depending on sampling (and whether a listener is registered or not), the passed activity may not be created.
        /// </summary>
        public static ActivityContext SafeGetContext([CanBeNull] this Activity activity)
        {
            if (activity != null)
            {
                return activity.Context;
            }

            if (Activity.Current != null)
            {
                return Activity.Current.Context;
            }

            return default;
        }
    }
}