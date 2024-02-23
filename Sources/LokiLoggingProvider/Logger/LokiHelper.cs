namespace LoggingProvider.Loki.Logger;

using System;

public static class LokiHelper
{
    public static event EventHandler<Exception>? ExceptionRaised;
    public static event EventHandler<int>? PushComplated;

    internal static void RaisePushComplated(object? sender, int count)
    {
        PushComplated?.Invoke(sender, count);
    }
    internal static void RaiseException(object? sender,Exception exception)
    {
        ExceptionRaised?.Invoke(sender, exception);
    }
}
