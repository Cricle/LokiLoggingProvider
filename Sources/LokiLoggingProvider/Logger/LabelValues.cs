namespace LoggingProvider.Loki.Logger;

using LoggingProvider.Loki.Options;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

internal class LabelValues : IDisposable
{
    private readonly PooledByteBufferWriter<KeyValuePair<string, string>> writer = new PooledByteBufferWriter<KeyValuePair<string, string>>(64);

    public ReadOnlyMemory<KeyValuePair<string, string>> Labels => writer.WrittenMemory;

    public LabelValues(LabelValues value)
    {
        if (value.writer.WrittenCount != 0)
        {
            var buffer = writer.GetMemory(value.writer.WrittenCount);
            value.writer.WrittenMemory.CopyTo(buffer);
            writer.Advance(value.writer.WrittenCount);
        }
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
        if (options.AdditionalStaticLabels.Count != 0)
        {

            var sp = writer.GetSpan(options.AdditionalStaticLabels.Count);
            var i = 0;
            foreach (var item in options.AdditionalStaticLabels)
            {
                string? value = item.Value?.ToString();
                if (item.Value != null)
                {
                    var key = item.Key.AsSpan().IndexOf(' ') == -1 ? item.Key : item.Key.Replace(" ", string.Empty);
                    sp[i] = new KeyValuePair<string, string>(key, value);
                    i++;
                }
            }
            writer.Advance(i);
        }
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
                if (item.Key != "{OriginalFormat}" && item.Value != null)
                {
                    if (!HasKey(item.Key))
                    {
                        Add(item.Key, item.Value!.ToString()!);
                    }
                }
            }
        }

        return labelValues;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool HasKey(string key)
    {
        var sp = writer.WrittenMemory.Span;
        for (int i = 0; i < sp.Length; i++)
        {
            if (sp[i].Key == key)
            {
                return true;
            }
        }
        return false;
    }
    public void Add(string key, string value)
    {
        if (!HasKey(key))
        {
            var sp = writer.GetSpan(1);
            sp[0] = new KeyValuePair<string, string>(key, value);
            writer.Advance(1);
        }
    }
    public void SetJob(string value)
    {
        Add("job", value);
    }

    public void SetInstance(string value)
    {
        Add("instance", value);
    }

    public void SetCategory(string value)
    {
        Add("category", value);
    }

    public void SetLogLevel(string value)
    {
        Add("level", value);
    }

    public void SetEventId(string value)
    {
        Add("eventId", value);
    }

    public void SetException(string value)
    {
        Add("exception", value);
    }

    public override string ToString()
    {
        return string.Join(",", writer.WrittenMemory.ToArray().Select(keyValuePair => $"{keyValuePair.Key}=\"{keyValuePair.Value}\""));
    }

    public void Dispose()
    {
        writer.Dispose();
    }
}
