namespace LoggingProvider.Loki.Formatters;

using System;
using System.Collections.Generic;
using System.Linq;
using LoggingProvider.Loki.Extensions;
using LoggingProvider.Loki.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

internal class LogfmtFormatter : ILogEntryFormatter
{
    private readonly LogfmtFormatterOptions formatterOptions;

    public LogfmtFormatter(LogfmtFormatterOptions formatterOptions)
    {
        this.formatterOptions = formatterOptions;
    }

    public string Format<TState>(LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider = null)
    {
        LogValues logValues = new();
        logValues.SetLogLevel(logEntry.LogLevel.ToString());

        if (formatterOptions.IncludeCategory)
        {
            logValues.SetCategory(logEntry.Category);
        }

        if (formatterOptions.IncludeEventId)
        {
            logValues.SetEventId(logEntry.EventId.Id);
        }

        logValues.SetMessage(logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception));

        if (logEntry.State is IEnumerable<KeyValuePair<string, object?>> state)
        {
            foreach (KeyValuePair<string, object?> keyValuePair in state)
            {
#if NETSTANDARD2_0
                if (!logValues.ContainsKey(keyValuePair.Key))
                {
                    logValues.Add(keyValuePair.Key, keyValuePair.Value);
                }
#else
                logValues.TryAdd(keyValuePair.Key, keyValuePair.Value);
#endif

            }
        }

        if (formatterOptions.IncludeScopes && scopeProvider != null)
        {
            scopeProvider.ForEachScope(
                (scope, state) =>
                {
                    if (scope is IEnumerable<KeyValuePair<string, object?>> keyValuePairs)
                    {
                        foreach (KeyValuePair<string, object?> keyValuePair in keyValuePairs)
                        {
#if NETSTANDARD2_0
                            if (!state.ContainsKey(keyValuePair.Key))
                            {
                                state.Add(keyValuePair.Key, keyValuePair.Value);
                            }
#else
                            state.TryAdd(keyValuePair.Key, keyValuePair.Value);
#endif
                        }
                    }
                },
                logValues);
        }

        if (logEntry.Exception != null)
        {
            logValues.SetException(logEntry.Exception.GetType());
        }

        if (formatterOptions.IncludeActivityTracking)
        {
            logValues.AddActivityTracking();
        }

        string message = string.Join(" ", logValues.Select(keyValuePair => $"{ToLogfmtKey(keyValuePair.Key)}={ToLogfmtValue(keyValuePair.Value)}"));

        if (logEntry.Exception != null && formatterOptions.PrintExceptions)
        {
            message += Environment.NewLine + logEntry.Exception.ToString();
        }

        return message;
    }

    private static string ToLogfmtKey(string key)
    {
        return key.Replace(" ", string.Empty);
    }

    private static string ToLogfmtValue(object? value)
    {
        string? stringValue = value?.ToString();

        if (string.IsNullOrEmpty(stringValue))
        {
            return "\"\"";
        }

        if (stringValue.Contains(' '))
        {
            return $"\"{stringValue}\"";
        }

        return stringValue ?? string.Empty;
    }
}
