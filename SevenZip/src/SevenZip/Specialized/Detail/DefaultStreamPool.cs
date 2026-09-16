using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace SevenZip.Specialized.Detail;

/// <summary>
/// The <see cref="DefaultStreamPool"/> supports the <see cref="IScopedStreamPool"/>
/// interface and allocates a block of managed memory that is referenced by the
/// streams created via this class.
/// </summary>
internal sealed class DefaultStreamPool : IScopedStreamPool
{
    private readonly int _capacity;
    private readonly byte[] _buffer;
    private int _cursor;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultStreamPool"/> class.
    /// </summary>
    /// <param name="capacity">
    /// The capacity of the memory to use for the streams.
    /// </param>
    public DefaultStreamPool(int capacity)
    {
        _capacity = capacity;
        _buffer = new byte[capacity];
    }

    public int Capacity => _capacity;

    /// <inheritdoc />
    public void Dispose()
    {
        GC.Collect(GC.GetGeneration(_buffer), GCCollectionMode.Forced);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CanCreateStreamWithCapacity(int capacity) =>
        _capacity - _cursor >= capacity;

    private void EnsureCapacity(int capacity)
    {
        if (CanCreateStreamWithCapacity(capacity))
        {
            return;
        }

        throw new OutOfMemoryException("The pool does not have enough remaining capacity to create another stream.");
    }

    #region IScopedStreamPool
    void IScopedStreamPool.Discard() => _cursor = 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool IScopedStreamPool.CanCreateStreamWithCapacity(int capacity) =>
        CanCreateStreamWithCapacity(capacity);

    Stream IScopedStreamPool.CreateStream(int capacity)
    {
        EnsureCapacity(capacity);

        var offset = _cursor;
        var stream = new MemoryStream(_buffer, offset, capacity);

        _cursor += capacity;
        return stream;
    }
    #endregion
}