namespace LoggingProvider.Loki.Formatters;

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using LoggingProvider.Loki.Extensions;
using LoggingProvider.Loki.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

[JsonSerializable(typeof(LogValues))]
internal partial class LogValuesJsonSerializerContext : JsonSerializerContext { }

internal class JsonFormatter : ILogEntryFormatter
{
    private readonly JsonFormatterOptions formatterOptions;

    private readonly JsonSerializerOptions serializerOptions;

    public JsonFormatter(JsonFormatterOptions formatterOptions)
    {
        this.formatterOptions = formatterOptions;

        serializerOptions = LogValuesJsonSerializerContext.Default.Options;
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

        if (logEntry.State is IEnumerable<KeyValuePair<string, object?>> state && state.Any())
        {
            try
            {
#if NETSTANDARD2_0
                logValues.SetState(state.ToDictionary(x=>x.Key,x=>x.Value));
#else
                logValues.SetState(new Dictionary<string, object?>(state));
#endif
            }
            catch
            {
                logValues.SetState(state);
            }
        }

        if (formatterOptions.IncludeScopes && scopeProvider != null)
        {
            List<object?> scopes = new();

            scopeProvider.ForEachScope(
                (scope, state) =>
                {
                    if (scope is IEnumerable<KeyValuePair<string, object?>> keyValuePairs)
                    {
                        try
                        {
#if NETSTANDARD2_0
                            state.Add(keyValuePairs.ToDictionary(x => x.Key, x => x.Value));
#else
                            state.Add(new Dictionary<string, object?>(keyValuePairs));
#endif
                            return;
                        }
                        catch
                        {
                            state.Add(keyValuePairs);
                            return;
                        }
                    }

                    state.Add(scope);
                },
                scopes);

            if (scopes.Any())
            {
                logValues.SetScopes(scopes);
            }
        }

        if (logEntry.Exception != null)
        {
            logValues.SetException(logEntry.Exception.GetType().ToString());
            logValues.SetExceptionDetails(logEntry.Exception.ToString());
        }

        if (formatterOptions.IncludeActivityTracking)
        {
            logValues.AddActivityTracking();
        }

        return JsonSerializer.Serialize(logValues, serializerOptions);
    }
}
