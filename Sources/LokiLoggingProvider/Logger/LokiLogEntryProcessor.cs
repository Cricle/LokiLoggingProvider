namespace LoggingProvider.Loki.Logger;

using LoggingProvider.Loki.Options;
using LoggingProvider.Loki.PushClients;
using System;

internal sealed class LokiLogEntryProcessor : ILokiLogEntryProcessor
{
    private readonly CycleLengthArray cycleLengthArray;

    public LokiLogEntryProcessor(ILokiPushClient client,BatchOptions batchOptions)
    {
        cycleLengthArray = new CycleLengthArray(client, batchOptions.BatchSize, batchOptions.Period);
    }

    public void Dispose()
    {
        cycleLengthArray.Dispose();
    }

    public void EnqueueMessage(LokiLogEntry message)
    {
        try
        {
            cycleLengthArray.Add(message);
        }
        catch (InvalidOperationException ex)
        {
            LokiHelper.RaiseException(this, ex);
        }
    }
}
