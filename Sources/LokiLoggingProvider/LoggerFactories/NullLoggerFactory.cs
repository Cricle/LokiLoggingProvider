namespace LoggingProvider.Loki.LoggerFactories;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

internal sealed class NullLoggerFactory : ILokiLoggerFactory
{
    public ILogger CreateLogger(string categoryName)
    {
        return NullLogger.Instance;
    }

    public void Dispose()
    {
        // Do nothing
    }

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        // Do nothing
    }
}
