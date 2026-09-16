using System;
using System.Linq;

namespace SevenZip.Specialized;

/// <summary>
/// Factory class for the <see cref="IScopedStreamPool"/> interface.
/// </summary>
internal static class ScopedStreamPoolFactory
{
    /// <summary>
    /// Gets the maximum capacity for the internal memory reserved for the streams.
    /// </summary>
    /// <remarks>
    /// The limit is documented in the <a href="https://learn.microsoft.com/en-us/dotnet/api/system.array?view=net-8.0&redirectedfrom=MSDN#remarks">
    /// Array Class - Remarks</a> section.
    /// </remarks>
    public const int MaxCapacity = 0x7FFFFFC7;

    /// <summary>
    /// Creates a new instance of the <see cref="IScopedStreamPool"/> interface which either uses
    /// managed or native memory and optionally determines the pool capacity based on the
    /// extracted size of the given archive.
    /// </summary>
    /// <param name="reader">
    /// The reader to obtain the archive size from, if the <paramref name="capacity"/>
    /// is not set to a specific value.
    /// </param>
    /// <param name="useNativeMemory">
    /// <c>true</c> to use native memory for the pool buffer, or <c>false</c> to rely
    /// on managed memory.
    /// </param>
    /// <param name="capacity">
    /// The capacity of the stream-pool or <c>0</c>, to automatically determine the
    /// capacity based on the uncompressed archive size.
    /// </param>
    /// <returns>
    /// A new instance of the <see cref="IScopedStreamPool"/> interface.
    /// </returns>
    public static IScopedStreamPool Create(ArchiveReader reader, bool useNativeMemory, int capacity = 0)
    {
        return Create(useNativeMemory, reader.GetStreamPoolCapacity(capacity));
    }

    /// <summary>
    /// Creates a new instance of the <see cref="IScopedStreamPool"/> interface which either uses
    /// managed or native memory.
    /// </summary>
    /// <param name="useNativeMemory">
    /// <c>true</c> to use native memory for the pool buffer, or <c>false</c> to rely
    /// on managed memory.
    /// </param>
    /// <param name="capacity">
    /// The capacity of the stream pool. This value MUST be greater than zero and should be
    /// big enough to store at least the largest file in the archive.
    /// </param>
    /// <returns>
    /// A new instance of the <see cref="IScopedStreamPool"/> interface.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="capacity"/> is less than 1.
    /// </exception>
    public static IScopedStreamPool Create(bool useNativeMemory, int capacity)
    {
        if (capacity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "The given StreamPool capacity must be greater than zero.");
        }

        return useNativeMemory
            ? new Detail.NativeStreamPool(capacity)
            : new Detail.DefaultStreamPool(capacity);
    }

    private static int GetStreamPoolCapacity(this ArchiveReader reader, int capacity)
    {
        if (capacity > 0)
        {
            return Math.Min(capacity, MaxCapacity);
        }

        return reader.GetUncompressedArchiveSize() switch
        {
            > MaxCapacity => MaxCapacity,
            var value => (int)value
        };
    }

    private static ulong GetUncompressedArchiveSize(this ArchiveReader reader)
    {
        return reader.Entries.Aggregate(0UL, (current, entry) => current + entry.UncompressedSize);
    }
}