namespace LoggingProvider.Loki.Logger;

using LoggingProvider.Loki.PushClients;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Threading;

internal readonly struct SwapResult<T> : IDisposable
{
    public static SwapResult<T> NotSwapped = new SwapResult<T>(null, 0);

    public SwapResult(T[]? array, int size)
    {
        Array = array;
        Size = size;
    }

    public T[]? Array { get; }

    public int Size { get; }

    public bool Swapped => Size != 0;

    public void Dispose()
    {
        if (Array != null)
        {
            ArrayPool<T>.Shared.Return(Array);
        }
    }
}
internal sealed class ThreadSafeFixedSizeArray<T> : IDisposable
{
    private T[]? array;
    private int index;

    private readonly int size;
    private readonly object locker = new object();

    public ThreadSafeFixedSizeArray(int size)
    {
        this.size = size;
        array = ArrayPool<T>.Shared.Rent(size);
        index = 0;
    }

    // 定义一个Add方法，作用是添加元素
    public SwapResult<T> Add(T element)
    {
        SwapResult<T> switched = SwapResult<T>.NotSwapped;
        lock (locker)
        {
            array![index] = element;
            index++;
            if (index >= size)
            {
                switched = new SwapResult<T>(array, index);
                array = ArrayPool<T>.Shared.Rent(size);
                index = 0;
            }
        }
        return switched;
    }

    public SwapResult<T> Swap()
    {
        lock (locker)
        {
            var switched = new SwapResult<T>(array, index);
            array = ArrayPool<T>.Shared.Rent(size);
            index = 0;
            return switched;
        }
    }

    public void Dispose()
    {
        if (array != null)
        {
            ArrayPool<T>.Shared.Return(array);
            array = null;
        }
    }
}

internal sealed class CycleLengthArray : IDisposable
{
    private ThreadSafeFixedSizeArray<LokiLogEntry> currentEntities;
    private readonly Thread backgroundThread;
    private readonly CancellationTokenSource rootToken = new CancellationTokenSource();
    private TaskCompletionSource<SwapResult<LokiLogEntry>>? innerTaskCompletionSource;

    public CycleLengthArray(ILokiPushClient pushClient, int limitCount, TimeSpan period)
    {
        this.PushClient = pushClient;
        this.LimitCount = limitCount;
        this.Period = period;
        currentEntities = new ThreadSafeFixedSizeArray<LokiLogEntry>(limitCount);
        backgroundThread = new Thread(LoopSend)
        {
            Name = "Loki logging sender"
        };

        backgroundThread.Start();
    }

    public ILokiPushClient PushClient { get; }

    public int LimitCount { get; }

    public TimeSpan Period { get; }

    private async void LoopSend()
    {
        var period = Period;
        while (!rootToken.IsCancellationRequested)
        {
            innerTaskCompletionSource = new TaskCompletionSource<SwapResult<LokiLogEntry>>();
            var delayTask = Task.Delay(period);
            var res = await Task.WhenAny(delayTask, innerTaskCompletionSource.Task);
            LokiLogEntry[] array;
            int length;
            SwapResult<LokiLogEntry>? result;
            if (res == innerTaskCompletionSource.Task)
            {
                result = innerTaskCompletionSource.Task.Result;
                length = result.Value.Size;
                array = result.Value.Array!;
            }
            else
            {
                var swapRes = currentEntities.Swap();
                length = swapRes.Size;
                array = swapRes.Array!;
                result = swapRes;
            }
            try
            {
                await PushClient.PushAsync(array, 0, length);
                LokiHelper.RaisePushComplated(this, length);
            }
            catch (Exception ex)
            {
                LokiHelper.RaiseException(this, ex);
            }
            finally
            {
                result?.Dispose();
            }
        }
    }

    public void Add(LokiLogEntry entity)
    {
        var source = innerTaskCompletionSource;
        var res = currentEntities.Add(entity);
        if (res.Swapped)
        {
            source?.TrySetResult(res);
        }
    }

    public void Dispose()
    {
        rootToken.Cancel();

        try
        {
            backgroundThread.Join(1500);
            currentEntities.Dispose();
        }
        catch (ThreadStateException ex)
        {
            // Do nothing
            Debug.WriteLine(ex);
            LokiHelper.RaiseException(this, ex);
        }
    }
}
