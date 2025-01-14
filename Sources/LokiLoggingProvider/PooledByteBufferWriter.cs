﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace LoggingProvider.Loki;

using System.Buffers;
using System.Diagnostics;

internal sealed class PooledByteBufferWriter<T> : IBufferWriter<T>, IDisposable
{
    // This class allows two possible configurations: if rentedBuffer is not null then
    // it can be used as an IBufferWriter and holds a buffer that should eventually be
    // returned to the shared pool. If rentedBuffer is null, then the instance is in a
    // cleared/disposed state and it must re-rent a buffer before it can be used again.
    private T[]? _rentedBuffer;
    private int _index;

    private const int MinimumBufferSize = 256;

    // Value copied from Array.MaxLength in System.Private.CoreLib/src/libraries/System.Private.CoreLib/src/System/Array.cs.
    public const int MaximumBufferSize = 0X7FFFFFC7;

    public PooledByteBufferWriter(int initialCapacity)
    {
        Debug.Assert(initialCapacity > 0);

        _rentedBuffer = ArrayPool<T>.Shared.Rent(initialCapacity);
        _index = 0;
    }

    public ReadOnlyMemory<T> WrittenMemory
    {
        get
        {
            Debug.Assert(_rentedBuffer != null);
            Debug.Assert(_index <= _rentedBuffer.Length);
            return _rentedBuffer.AsMemory(0, _index);
        }
    }

    public int WrittenCount
    {
        get
        {
            Debug.Assert(_rentedBuffer != null);
            return _index;
        }
    }

    public int Capacity
    {
        get
        {
            Debug.Assert(_rentedBuffer != null);
            return _rentedBuffer.Length;
        }
    }

    public int FreeCapacity
    {
        get
        {
            Debug.Assert(_rentedBuffer != null);
            return _rentedBuffer.Length - _index;
        }
    }

    public T[] DangerouGetArray()
    {
        if (_rentedBuffer==null)
        {
            return Array.Empty<T>();
        }
        return _rentedBuffer;
    }

    public void Clear()
    {
        ClearHelper();
    }

    public void ClearAndReturnBuffers()
    {
        Debug.Assert(_rentedBuffer != null);

        ClearHelper();
        T[] toReturn = _rentedBuffer;
        _rentedBuffer = null;
        ArrayPool<T>.Shared.Return(toReturn);
    }

    private void ClearHelper()
    {
        Debug.Assert(_rentedBuffer != null);
        Debug.Assert(_index <= _rentedBuffer.Length);

        _rentedBuffer.AsSpan(0, _index).Clear();
        _index = 0;
    }

    // Returns the rented buffer back to the pool
    public void Dispose()
    {
        if (_rentedBuffer == null)
        {
            return;
        }

        ClearHelper();
        T[] toReturn = _rentedBuffer;
        _rentedBuffer = null;
        ArrayPool<T>.Shared.Return(toReturn);
    }

    public void InitializeEmptyInstance(int initialCapacity)
    {
        Debug.Assert(initialCapacity > 0);
        Debug.Assert(_rentedBuffer is null);

        _rentedBuffer = ArrayPool<T>.Shared.Rent(initialCapacity);
        _index = 0;
    }

    public void Advance(int count)
    {
        Debug.Assert(_rentedBuffer != null);
        Debug.Assert(count >= 0);
        Debug.Assert(_index <= _rentedBuffer.Length - count);
        _index += count;
    }

    public Memory<T> GetMemory(int sizeHint = MinimumBufferSize)
    {
        CheckAndResizeBuffer(sizeHint);
        return _rentedBuffer.AsMemory(_index);
    }

    public Span<T> GetSpan(int sizeHint = MinimumBufferSize)
    {
        CheckAndResizeBuffer(sizeHint);
        return _rentedBuffer.AsSpan( _index);
    }

    private void CheckAndResizeBuffer(int sizeHint)
    {
        Debug.Assert(_rentedBuffer != null);
        Debug.Assert(sizeHint > 0);

        int currentLength = _rentedBuffer.Length;
        int availableSpace = currentLength - _index;

        // If we've reached ~1GB written, grow to the maximum buffer
        // length to avoid incessant minimal growths causing perf issues.
        if (_index >= MaximumBufferSize / 2)
        {
            sizeHint = Math.Max(sizeHint, MaximumBufferSize - currentLength);
        }

        if (sizeHint > availableSpace)
        {
            int growBy = Math.Max(sizeHint, currentLength);

            int newSize = currentLength + growBy;

            if ((uint)newSize > MaximumBufferSize)
            {
                newSize = currentLength + sizeHint;
                if ((uint)newSize > MaximumBufferSize)
                {
                    ThrowHelper.ThrowOutOfMemoryException_BufferMaximumSizeExceeded((uint)newSize);
                }
            }

            T[] oldBuffer = _rentedBuffer;

            _rentedBuffer = ArrayPool<T>.Shared.Rent(newSize);

            Debug.Assert(oldBuffer.Length >= _index);
            Debug.Assert(_rentedBuffer.Length >= _index);

            Span<T> oldBufferAsSpan = oldBuffer.AsSpan(0, _index);
            oldBufferAsSpan.CopyTo(_rentedBuffer);
            oldBufferAsSpan.Clear();
            ArrayPool<T>.Shared.Return(oldBuffer);
        }

        Debug.Assert(_rentedBuffer.Length - _index > 0);
        Debug.Assert(_rentedBuffer.Length - _index >= sizeHint);
    }
}
