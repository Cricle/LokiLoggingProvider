namespace LoggingProvider.Loki.Options;

using System;

public class BatchOptions
{
    public int BatchSize { get; set; } = 1000;

    public TimeSpan Period { get; set; } = TimeSpan.FromSeconds(2);
}
