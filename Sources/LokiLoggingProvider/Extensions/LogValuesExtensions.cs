namespace LoggingProvider.Loki.Extensions;

using LoggingProvider.Loki.Formatters;
using System.Diagnostics;

internal static class LogValuesExtensions
{
    public static LogValues AddActivityTracking(this LogValues logValues)
    {
        if (Activity.Current is Activity activity)
        {
#if NETSTANDARD2_0
            if (!logValues.ContainsKey("SpanId")) logValues.Add("SpanId", activity.GetSpanId());
            if (!logValues.ContainsKey("TraceId")) logValues.Add("SpanId", activity.GetTraceId());
            if (!logValues.ContainsKey("ParentId")) logValues.Add("SpanId", activity.GetParentId());
#else
            logValues.TryAdd("SpanId", activity.GetSpanId());
            logValues.TryAdd("TraceId", activity.GetTraceId());
            logValues.TryAdd("ParentId", activity.GetParentId());
#endif
        }

        return logValues;
    }
}
