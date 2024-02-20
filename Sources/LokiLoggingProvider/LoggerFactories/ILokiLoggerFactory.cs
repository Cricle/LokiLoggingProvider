namespace LoggingProvider.Loki.LoggerFactories;

using System;
using Microsoft.Extensions.Logging;

internal interface ILokiLoggerFactory : IDisposable
{
    ILogger CreateLogger(string categoryName);

    void SetScopeProvider(IExternalScopeProvider scopeProvider);
}
