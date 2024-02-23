namespace LoggingProvider.Loki.Formatters;

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
#if NETSTANDARD2_0
using System.Text;
#endif
using LoggingProvider.Loki.Extensions;
using LoggingProvider.Loki.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

internal class SimpleFormatter : ILogEntryFormatter
{
    private readonly SimpleFormatterOptions formatterOptions;

    public SimpleFormatter(SimpleFormatterOptions formatterOptions)
    {
        this.formatterOptions = formatterOptions;
    }

    public string Format<TState>(LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider = null)
    {
#if NETSTANDARD2_0

        var message = new StringBuilder($"[{GetLogLevelString(logEntry.LogLevel)}] ");

        if (formatterOptions.IncludeActivityTracking && Activity.Current is Activity activity)
        {
            message.Append(activity.GetTraceId());
            message.Append(" - ");
        }

        message.Append(logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception) ?? "Something happened.");

        if (logEntry.Exception != null)
        {
            message.AppendLine();
            message.Append(logEntry.Exception.ToString());
        }

        return message.ToString();
#else
        var def = new DefaultInterpolatedStringHandler();
        def.AppendLiteral("[");
        def.AppendLiteral(GetLogLevelString(logEntry.LogLevel));
        def.AppendLiteral("}");
        if (formatterOptions.IncludeActivityTracking && Activity.Current is Activity activity)
        {
            def.AppendLiteral(activity.GetTraceId()!);
            def.AppendLiteral(" - ");
        }

        def.AppendLiteral(logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception) ?? "Something happened.");

        if (logEntry.Exception != null)
        {
            def.AppendLiteral(Environment.NewLine);
            def.AppendLiteral(logEntry.Exception.ToString());
        }

        return def.ToStringAndClear();
#endif
    }

    private static string GetLogLevelString(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "TRCE",
            LogLevel.Debug => "DBUG",
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "EROR",
            LogLevel.Critical => "CRIT",
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel)),
        };
    }
}
