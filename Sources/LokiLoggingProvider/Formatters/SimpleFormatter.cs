namespace LoggingProvider.Loki.Formatters;

using System;
using System.Diagnostics;
using System.Text;
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
