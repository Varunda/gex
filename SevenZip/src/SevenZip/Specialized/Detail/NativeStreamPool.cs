using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SevenZip.Specialized.Detail;

/// <summary>
/// The <see cref="NativeStreamPool"/> supports the <see cref="IScopedStreamPool"/>
/// interface and allocates a block of native memory which is referenced by the
/// streams created via this class.
/// 
/// Using native memory <c>may</c> reduce the workload of garbage collector when
/// working with large archive.
/// </summary>
internal sealed class NativeStreamPool : IScopedStreamPool
{
    private readonly IntPtr _pointer;
    private readonly int _capacity;
    private int _cursor;

    /// <summary>
    /// Initializes a new instance of the <see cref="NativeStreamPool"/> class.
    /// </summary>
    /// <param name="capacity">
    /// The capacity of the memory to use for the streams.
    /// </param>
    public unsafe NativeStreamPool(int capacity)
    {
        _capacity = capacity;
        _pointer =
#if NET6_0_OR_GREATER
            new IntPtr(NativeMemory.Alloc((nuint)capacity))
#else
            Marshal.AllocHGlobal(capacity)
#endif
            ;

        GC.AddMemoryPressure(capacity);
    }

    public int Capacity => _capacity;

    /// <inheritdoc />
    public unsafe void Dispose()
    {
#if NET6_0_OR_GREATER
        NativeMemory.Free(_pointer.ToPointer())
#else
        Marshal.FreeHGlobal(_pointer)
#endif
        ;

        GC.RemoveMemoryPressure(_capacity);
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

    bool IScopedStreamPool.CanCreateStreamWithCapacity(int capacity) =>
        CanCreateStreamWithCapacity(capacity);

    Stream IScopedStreamPool.CreateStream(int capacity)
    {
        EnsureCapacity(capacity);

        unsafe
        {
            var pointer = new IntPtr(Unsafe.Add<byte>(_pointer.ToPointer(), _cursor));
            var stream = new UnmanagedMemoryStream((byte *)pointer, 0L, capacity, FileAccess.ReadWrite);

            _cursor += capacity;

            return stream;
        }
    }
    #endregion
}
