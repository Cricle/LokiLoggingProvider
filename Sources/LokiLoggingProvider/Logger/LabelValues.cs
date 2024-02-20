namespace LoggingProvider.Loki.Logger;

using System;
using System.Collections.Generic;
using System.Linq;
using LoggingProvider.Loki.Options;
using Microsoft.Extensions.Logging.Abstractions;

internal class LabelValues : Dictionary<string, string>
{
    public LabelValues()
    {
    }

    public LabelValues(StaticLabelOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.JobName))
        {
            SetJob(options.JobName);
        }

        if (options.IncludeInstanceLabel)
        {
            SetInstance(Environment.MachineName);
        }

        foreach (KeyValuePair<string, object?> label in options.AdditionalStaticLabels)
        {
            string? value = label.Value?.ToString();

            if (value != null)
            {
#if NETSTANDARD2_0
                var key = label.Key.Replace(" ", string.Empty);
                if (!ContainsKey(key))
                {
                    this[key] = value;
                }
#else
                TryAdd(label.Key.Replace(" ", string.Empty), value);
#endif
            }
        }
    }

    private LabelValues(IDictionary<string, string> dictionary)
        : base(dictionary)
    {
    }

    public LabelValues AddDynamicLabels<TState>(DynamicLabelOptions options, LogEntry<TState> logEntry)
    {
        LabelValues labelValues = new(this);

        if (options.IncludeCategory)
        {
            labelValues.SetCategory(logEntry.Category);
        }

        if (options.IncludeLogLevel)
        {
            labelValues.SetLogLevel(logEntry.LogLevel.ToString());
        }

        if (options.IncludeEventId)
        {
            labelValues.SetEventId(logEntry.EventId.ToString());
        }

        if (options.IncludeException && logEntry.Exception != null)
        {
            labelValues.SetException(logEntry.Exception.GetType().ToString());
        }

        if (options.IncludeDynamicTags && logEntry.State is IEnumerable<KeyValuePair<string, object>> tags)
        {
            foreach (var item in tags)
            {
                if (item.Key!= "{OriginalFormat}" && item.Value != null)
                {

#if NETSTANDARD2_0
                    if (!labelValues.ContainsKey(item.Key))
                    {
                        labelValues.Add(item.Key, item.Value.ToString()!);
                    }
#else

                    labelValues.TryAdd(item.Key, item.Value!.ToString()!);
#endif
                }
            }
        }

        return labelValues;
    }

    public void SetJob(string value)
    {
        this["job"] = value;
    }

    public void SetInstance(string value)
    {
        this["instance"] = value;
    }

    public void SetCategory(string value)
    {
        this["category"] = value;
    }

    public void SetLogLevel(string value)
    {
        this["level"] = value;
    }

    public void SetEventId(string value)
    {
        this["eventId"] = value;
    }

    public void SetException(string value)
    {
        this["exception"] = value;
    }

    public override string ToString()
    {
        return string.Join(",", this.Select(keyValuePair => $"{keyValuePair.Key}=\"{keyValuePair.Value}\""));
    }
}
