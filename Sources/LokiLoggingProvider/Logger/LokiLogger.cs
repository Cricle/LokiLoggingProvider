namespace LoggingProvider.Loki.Logger;

using System;
using LoggingProvider.Loki.Formatters;
using LoggingProvider.Loki.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

internal class LokiLogger : ILogger
{
    private readonly string categoryName;

    private readonly ILogEntryFormatter formatter;

    private readonly ILokiLogEntryProcessor processor;

    private readonly LabelValues staticLabels;

    private readonly DynamicLabelOptions dynamicLabelOptions;

    public LokiLogger(
        string categoryName,
        ILogEntryFormatter formatter,
        ILokiLogEntryProcessor processor,
        StaticLabelOptions staticLabelOptions,
        DynamicLabelOptions dynamicLabelOptions)
    {
        this.categoryName = categoryName;
        this.formatter = formatter;
        this.processor = processor;

        staticLabels = new LabelValues(staticLabelOptions);
        this.dynamicLabelOptions = dynamicLabelOptions;
    }

    internal IExternalScopeProvider ScopeProvider { get; set; } = NullExternalScopeProvider.Instance;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return ScopeProvider.Push(state);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        LogEntry<TState> logEntry = new(logLevel, categoryName, eventId, state, exception, formatter);

        var timestamp = DateTimeOffset.Now;
        LabelValues labels = staticLabels.AddDynamicLabels(dynamicLabelOptions, logEntry);
        string message = this.formatter.Format(logEntry, ScopeProvider);

        processor.EnqueueMessage(new LokiLogEntry(timestamp, labels, message));
    }
}
