namespace LoggingProvider.Loki.Extensions;

using LoggingProvider.Loki.Formatters;
using LoggingProvider.Loki.LoggerFactories;
using LoggingProvider.Loki.Options;

internal static class LokiLoggerOptionsExtensions
{
    public static ILogEntryFormatter CreateFormatter(this LokiLoggerOptions options)
    {
        return options.Formatter switch
        {
            Formatter.Json => new JsonFormatter(options.JsonFormatter),
            Formatter.Logfmt => new LogfmtFormatter(options.LogfmtFormatter),
            _ => new SimpleFormatter(options.SimpleFormatter),
        };
    }

    public static ILokiLoggerFactory CreateLoggerFactory(this LokiLoggerOptions options)
    {
        ILogEntryFormatter formatter = options.CreateFormatter();

        return options.Client switch
        {
            PushClient.Http => new HttpLoggerFactory(
                options.Http,
                options.StaticLabels,
                options.DynamicLabels,
                formatter),

            _ => new NullLoggerFactory(),
        };
    }
}
