using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using SevenZip.Detail;

namespace SevenZip;

/// <summary>
/// The <see cref="ExtractTransaction"/> class can be used to perform multiple separate extractions
/// without the need to reopen the internal archive once per call, but maintain one archive handle
/// until the transaction object is being disposed again.
/// 
/// This allows more efficient extract operations for scenarios where <c>Extract</c> has to be
/// called multiple times.
/// 
/// <para>
/// The <see cref="ExtractTransaction"/> object must be disposed as soon as it is no longer needed.
/// 
/// <code>
/// using var reader = new ArchiveReader(stream, leaveOpen: true);
/// 
/// {
///   using var transaction = new ExtractTransaction(reader);
///   
///   foreach (var (index, stream) in EnumerateEntriesToExtract())
///   {
///     transaction.Extract(index, stream);
///   }
/// }
/// </code>
/// </para>
/// </summary>
public sealed class ExtractTransaction : IDisposable
{
    private readonly ThreadLocal<uint[]> _indices = new ThreadLocal<uint[]>(trackAllValues: false);
    private readonly ConcurrentDictionary<int, Stream> _streams;
    private readonly ExtractContext _context;
    private readonly ArchiveReader _reader;
    private readonly OpenArchiveGuard _guard;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractTransaction"/> class.
    /// </summary>
    /// <param name="reader">
    /// The <see cref="ArchiveReader"/> to work with.
    /// </param>
    /// <param name="flags">
    /// Additional flags to configure the extraction behavior. See <see cref="ArchiveFlags"/> for details.
    /// </param>
    /// <param name="streamCacheCapacity">
    /// The initial capacity of the internal stream dictionary.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="reader"/> is <c>null</c>.
    /// </exception>
    public ExtractTransaction(ArchiveReader reader, ArchiveFlags flags = ArchiveFlags.None, int streamCacheCapacity = 0)
    {
        _streams = new(1, streamCacheCapacity);
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        (_guard, _context) = _reader.CreateTransaction(OnGetStream, flags);
    }

    /// <summary>
    /// Extracts the entry with the given <paramref name="index"/> to the <paramref name="stream"/>.
    /// 
    /// <para>
    /// The stream passed to this method is only disposed after use if this instance has been
    /// created with the <see cref="ArchiveFlags.CloseArchiveEntryStreamAfterExtraction"/> flag.
    /// </para>
    /// </summary>
    /// <param name="index">
    /// The index of the archive entry to extract.
    /// </param>
    /// <param name="stream">
    /// The stream to write the archive entry to.
    /// </param>
    /// <exception cref="ArgumentException">Thrown if the index is out of range.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the object has already been disposed.</exception>
    /// <exception cref="Exception">
    /// A more generic exception may be thrown when the native library reports an error.
    /// The <see cref="Exception.HResult"/> property may be used to retrieve the error code.
    /// </exception>
    public void Extract(int index, Stream stream)
    {
        EnsureNotNull(stream, nameof(stream));
        EnsureNotDisposed();

        if (_indices.IsValueCreated is false)
        {
            _indices.Value = new uint[4];
        }

        _indices.Value[0] = (uint)index;
        _streams[index] = stream;
        _reader.Extract(_indices.Value, 1, _context);
    }

    /// <summary>
    /// Extracts the entries with the given <paramref name="indices"/> to a stream returned
    /// by the <paramref name="onGetStream"/> callback.
    /// <para>
    /// The streams passed to this method are only disposed after use if this instance has been
    /// created with the <see cref="ArchiveFlags.CloseArchiveEntryStreamAfterExtraction"/> flag.
    /// </para>
    /// </summary>
    /// <param name="indices">
    /// The indices of the archive entries to extract.
    /// </param>
    /// <param name="onGetStream">
    /// A callback which is invoked to obtain the output stream for
    /// an archive entry. The stream is then used to write the uncompressed content.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="indices"/> or <paramref name="onGetStream"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown if the index is out of range.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the object has already been disposed.</exception>
    /// <exception cref="Exception">
    /// A more generic exception may be thrown when the native library reports an error.
    /// The <see cref="Exception.HResult"/> property may be used to retrieve the error code.
    /// </exception>
    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    public void Extract(IEnumerable<int> indices, Func<int, Stream> onGetStream)
    {
        EnsureNotNull(indices, nameof(indices));
        EnsureNotNull(onGetStream, nameof(onGetStream));
        EnsureNotDisposed();

        var count = GetEnumeratedCount(indices);

        if (_indices.IsValueCreated == false || _indices.Value.Length < count)
        {
            _indices.Value = new uint[count];
        }

        var cursor = 0;

        foreach (var index in indices)
        {
            _indices.Value[cursor] = (uint)index;
            _streams[index] = onGetStream(index);

            cursor++;
        }

        _reader.Extract(_indices.Value, count, _context);
    }

    /// <summary>
    /// Extracts the entries with the given <paramref name="indices"/> to a stream returned
    /// by the <paramref name="onGetStream"/> callback.
    /// <para>
    /// The streams passed to this method are only disposed after use if this instance has been
    /// created with the <see cref="ArchiveFlags.CloseArchiveEntryStreamAfterExtraction"/> flag.
    /// </para>
    /// </summary>
    /// <param name="indices">
    /// The indices of the archive entries to extract.
    /// </param>
    /// <param name="onGetStream">
    /// A callback which is invoked to obtain the output stream for
    /// an archive entry. The stream is then used to write the uncompressed content.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="indices"/> or <paramref name="onGetStream"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown if the index is out of range.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the object has already been disposed.</exception>
    /// <exception cref="Exception">
    /// A more generic exception may be thrown when the native library reports an error.
    /// The <see cref="Exception.HResult"/> property may be used to retrieve the error code.
    /// </exception>
    public void Extract(IReadOnlyList<int> indices, Func<int, Stream> onGetStream)
    {
        EnsureNotNull(indices, nameof(indices));
        EnsureNotNull(onGetStream, nameof(onGetStream));
        EnsureNotDisposed();

        var count = indices.Count;

        if (_indices.IsValueCreated == false || _indices.Value.Length < count)
        {
            _indices.Value = new uint[count];
        }

        for (var cursor = 0; cursor < count; cursor++)
        {
            var index = indices[cursor];

            _indices.Value[cursor] = (uint)index;
            _streams[index] = onGetStream(index);
        }

        _reader.Extract(_indices.Value, count, _context);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _guard.Dispose();
        _streams.Clear();
        _context.Dispose();
    }

    private Stream OnGetStream(ArchiveEntry entry)
    {
        return _streams.TryRemove(entry.Index, out var stream)
            ? stream
            : null;
    }

    private void EnsureNotDisposed()
    {
        if (_disposed == false)
        {
            return;
        }

        throw new ObjectDisposedException(nameof(ExtractTransaction), "The object has already been disposed.");
    }

    private static void EnsureNotNull<T>(T value, string name) where T : class
    {
        if (value != null)
        {
            return;
        }

        throw new ArgumentNullException(name);
    }

    private static int GetEnumeratedCount<T>(IEnumerable<T> enumerable)
    {
        return 
#if NET6_0_OR_GREATER
            enumerable.TryGetNonEnumeratedCount(out var count) ? count : enumerable.Count()
#else
            enumerable.Count()
#endif
            ;
    }
}