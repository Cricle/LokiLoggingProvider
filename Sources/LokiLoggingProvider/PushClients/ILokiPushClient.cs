namespace LoggingProvider.Loki.PushClients;

using LoggingProvider.Loki.Logger;

internal interface ILokiPushClient
{
    void Push(LokiLogEntry entry);
}
