namespace LoggingProvider.Loki.Logger;

using System;

public static class LokiExceptionHelper
{
    public static event EventHandler<Exception>? ExceptionRaised;

    internal static void RaiseException(object? sender,Exception exception)
    {
        ExceptionRaised?.Invoke(sender, exception);
    }
}
