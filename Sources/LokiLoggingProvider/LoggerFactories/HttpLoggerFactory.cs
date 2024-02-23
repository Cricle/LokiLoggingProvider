namespace LoggingProvider.Loki.LoggerFactories;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using LoggingProvider.Loki.Formatters;
using LoggingProvider.Loki.Logger;
using LoggingProvider.Loki.Options;
using LoggingProvider.Loki.PushClients;
using Microsoft.Extensions.Logging;

internal sealed class HttpLoggerFactory : ILokiLoggerFactory
{
    private readonly ConcurrentDictionary<string, LokiLogger> loggers = new();

    private readonly HttpClient httpClient;

    private readonly LokiLogEntryProcessor processor;

    private readonly StaticLabelOptions staticLabelOptions;

    private readonly DynamicLabelOptions dynamicLabelOptions;

    private readonly ILogEntryFormatter formatter;

    private IExternalScopeProvider scopeProvider = NullExternalScopeProvider.Instance;

    private bool disposed;

    public HttpLoggerFactory(
        HttpOptions httpOptions,
        StaticLabelOptions staticLabelOptions,
        DynamicLabelOptions dynamicLabelOptions,
        BatchOptions batchOptions,
        ILogEntryFormatter formatter)
    {
        httpClient = new()
        {
            BaseAddress = new Uri(httpOptions.Address),
        };

        if (!string.IsNullOrEmpty(httpOptions.User) && !string.IsNullOrEmpty(httpOptions.Password))
        {
            byte[] credentials = Encoding.ASCII.GetBytes($"{httpOptions.User}:{httpOptions.Password}");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));
        }

        HttpPushClient pushClient = new(httpClient);
        processor = new LokiLogEntryProcessor(pushClient, batchOptions);

        this.staticLabelOptions = staticLabelOptions;
        this.dynamicLabelOptions = dynamicLabelOptions;

        this.formatter = formatter;
    }

    public ILogger CreateLogger(string categoryName)
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(HttpLoggerFactory));
        }

        return loggers.GetOrAdd(categoryName, name => new LokiLogger(
            name,
            formatter,
            processor,
            staticLabelOptions,
            dynamicLabelOptions)
        {
            ScopeProvider = scopeProvider,
        });
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        processor.Dispose();
        httpClient.Dispose();
        disposed = true;
    }

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        this.scopeProvider = scopeProvider;

        foreach (KeyValuePair<string, LokiLogger> logger in loggers)
        {
            logger.Value.ScopeProvider = this.scopeProvider;
        }
    }
}
