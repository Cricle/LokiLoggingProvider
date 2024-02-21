namespace LoggingProvider.Loki;

using System.Runtime.CompilerServices;

internal static partial class ThrowHelper
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowOutOfMemoryException_BufferMaximumSizeExceeded(uint capacity)
    {
        throw new OutOfMemoryException($"Out of capacity by {capacity}");
    }
}
