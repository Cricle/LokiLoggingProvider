namespace LoggingProvider.Loki.PushClients;

using LoggingProvider.Loki.Logger;

internal interface ILokiPushClient
{
    Task PushAsync(IReadOnlyList<LokiLogEntry> entries,int start,int length);
}
