namespace LoggingProvider.Loki;

using System;
using LoggingProvider.Loki.Extensions;
using LoggingProvider.Loki.LoggerFactories;
using LoggingProvider.Loki.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

[ProviderAlias("Loki")]
public sealed class LokiLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly IDisposable? onChangeToken;

    private bool disposed;

    private ILokiLoggerFactory loggerFactory;

    public LokiLoggerProvider(IOptionsMonitor<LokiLoggerOptions> options)
    {
        loggerFactory = options.CurrentValue.CreateLoggerFactory();

        onChangeToken = options.OnChange(updatedOptions =>
        {
            loggerFactory.Dispose();
            loggerFactory = updatedOptions.CreateLoggerFactory();
        });
    }

    public ILogger CreateLogger(string categoryName)
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(LokiLoggerProvider));
        }

        return loggerFactory.CreateLogger(categoryName);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        onChangeToken?.Dispose();
        loggerFactory.Dispose();

        disposed = true;
    }

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        loggerFactory.SetScopeProvider(scopeProvider);
    }
}
