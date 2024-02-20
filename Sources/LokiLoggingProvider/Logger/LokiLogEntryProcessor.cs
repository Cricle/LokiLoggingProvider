namespace LoggingProvider.Loki.Logger;

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using LoggingProvider.Loki.PushClients;

internal sealed class LokiLogEntryProcessor : ILokiLogEntryProcessor
{
    private const int MaxQueuedMessages = 1024;

    private readonly ILokiPushClient client;

    private readonly Thread backgroundThread;

    private bool disposed;

    public LokiLogEntryProcessor(ILokiPushClient client)
    {
        this.client = client;

        backgroundThread = new Thread(ProcessLogQueue)
        {
            Name = nameof(LokiLogEntryProcessor),
        };

        backgroundThread.Start();
    }

    // Internal for testing
    internal BlockingCollection<LokiLogEntry> MessageQueue { get; } = new BlockingCollection<LokiLogEntry>(MaxQueuedMessages);

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        MessageQueue.CompleteAdding();

        try
        {
            backgroundThread.Join();
        }
        catch (ThreadStateException ex)
        {
            // Do nothing
            Debug.WriteLine(ex);
        }

        disposed = true;
    }

    public void EnqueueMessage(LokiLogEntry message)
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(LokiLogEntryProcessor));
        }

        if (MessageQueue.IsAddingCompleted)
        {
            return;
        }

        try
        {
            MessageQueue.Add(message);
        }
        catch (InvalidOperationException ex)
        {
            // Do nothing
            Debug.WriteLine(ex);
        }
    }

    private void ProcessLogQueue()
    {
        try
        {
            foreach (LokiLogEntry entry in MessageQueue.GetConsumingEnumerable())
            {
                PushMessage(entry);
            }
        }
        catch
        {
            try
            {
                MessageQueue.CompleteAdding();
            }
            catch (Exception ex)
            {
                // Do nothing
                Debug.WriteLine(ex);
            }
        }
    }

    private void PushMessage(LokiLogEntry entry)
    {
        try
        {
            client.Push(entry);
        }
        catch (Exception ex)
        {
            // Do nothing
            Debug.WriteLine(ex);
        }
    }
}
