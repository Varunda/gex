using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SevenZip.Detail;

/// <summary>
/// The <see cref="NativeArray{T}"/> struct wraps a native pointer and allows accessing
/// it via the index operator, while the implementation takes care about computing the 
/// pointer offsets.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
internal readonly ref struct NativeArray<T> where T : struct
{
    private readonly nint _pointer;
    private readonly int _elementSize;

    /// <summary>
    /// A delegate to which the index and a reference to an element are passed.
    /// </summary>
    /// <param name="element"></param>
    public delegate void Action(int index, ref T element);

    /// <summary>
    /// Initializes a new instance of the <see cref="NativeArray{T}"/> struct.
    /// </summary>
    /// <param name="elementCount">The number of elements to store.</param>
    /// <param name="elementSize">The size a single element requires, in bytes.</param>
    public NativeArray(int elementCount, int elementSize)
    {
        Count = elementCount;

        _elementSize = elementSize;
        _pointer = Alloc(elementCount, elementSize);
    }

    /// <summary>
    /// Gets the number of elements the array can store.
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// Gets the native pointer to the array.
    /// </summary>
    public nint Pointer => _pointer;

    /// <summary>
    /// Gets the reference to the element at the given <paramref name="index"/>.
    /// </summary>
    /// <param name="index">The index of the element to return.</param>
    /// <returns>A reference to the element at the given <paramref name="index"/>.</returns>
    public unsafe ref T this[int index]
    {
        get
        {
            var pointer = _pointer + index * _elementSize;

            return ref Unsafe.AsRef<T>((void *)pointer);
        }
    }

    /// <summary>
    /// Frees the native memory.
    /// </summary>
    public void Dispose()
    {
        Free(_pointer);
    }

    /// <summary>
    /// Iterates over all elements of the given <paramref name="buffer"/> and invokes
    /// the <paramref name="callback"/> for each element.
    /// </summary>
    /// <param name="buffer">
    /// The buffer to iterate over.
    /// </param>
    /// <param name="callback">
    /// The callback to invoke for each element.
    /// </param>
    public static void ForEach(NativeArray<T> buffer, Action callback)
    {
        for (var i = 0; i < buffer.Count; i++)
        {
            callback(i, ref buffer[i]);
        }
    }

    private static unsafe nint Alloc(int elementCount, int elementSize)
    {
        var byteCount = elementCount * elementSize;

#if NET
        var pointer = NativeMemory.AllocZeroed((nuint)byteCount);
        return (nint)pointer;
#else
        var pointer = Marshal.AllocHGlobal(byteCount);

        Unsafe.InitBlockUnaligned((void *)pointer, 0x00, (uint)byteCount);
        return pointer;
#endif
    }

    private static unsafe void Free(nint pointer) =>
#if NET
        NativeMemory.Free((void *)pointer)
#else
        Marshal.FreeHGlobal(pointer)
#endif
        ;
}
