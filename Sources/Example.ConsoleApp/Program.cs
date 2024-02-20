namespace Example.ConsoleApp;

using LoggingProvider.Loki;
using Microsoft.Extensions.Logging;

public class Program
{
    protected Program()
    {
    }

    public static void Main()
    {
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddLoki(configure => configure.Client =  LoggingProvider.Loki.Options.PushClient.Http);
        });

        ILogger<Program> logger = loggerFactory.CreateLogger<Program>();

        logger.LogInformation("Hello from my Console App!");
    }
}
