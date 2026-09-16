using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using SevenZip.Detail;
using SevenZip.Interop;

namespace SevenZip;

/// <summary>
/// The <see cref="ArchiveReader"/> allows browsing and extracting the content of an archive.
/// It makes use of the native <c>7z</c> library, which is loaded when creating an instance
/// of this class.
/// </summary>
public sealed class ArchiveReader : IDisposable
{
    private readonly IArchiveReaderDelegate _delegate;
    private readonly IntPtr _libraryHandle;
    private readonly ArchiveFormat _format;
    private readonly IArchiveReader _reader;
    private readonly ArchiveStream _stream;
    private readonly int _offset;
    private readonly ArchiveEntry[] _entries;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveReader"/> class which opens the given
    /// <paramref name="path"/> to read the archive content.
    /// </summary>
    /// <param name="path">
    /// The path to the archive to load.
    /// </param>
    /// <param name="delegate">
    /// An optional instance of the <see cref="IArchiveReaderDelegate"/>
    /// interface, which is used to report the progress or errors during extraction.
    /// </param>
    /// <param name="config">
    /// Optional configuration parameters.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="path"/> is <c>null</c>.</exception>
    /// <exception cref="IOException">Thrown if the archive-file could not be opened.</exception>
    /// <exception cref="NotSupportedException">Thrown if the archive format could not be determined.</exception>
    /// <exception cref="BadImageFormatException">Thrown if the native library could not be loaded.</exception>
    /// <exception cref="DllNotFoundException">Thrown if the native library could not be found.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the archive open operation failed.</exception>
    public ArchiveReader(string path, IArchiveReaderDelegate @delegate = null, ArchiveConfig config = null)
        : this(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), leaveOpen: false, @delegate, config)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveReader"/> class which uses the given
    /// <paramref name="stream"/> to read the archive content.
    /// </summary>
    /// <param name="stream">
    /// The stream to read the archive data from.
    /// </param>
    /// <param name="leaveOpen">
    /// If set to <c>true</c>, the stream will not be disposed when this
    /// instance is being closed. Otherwise, the stream's <c>Dispose</c> method will be called.
    /// </param>
    /// <param name="delegate">
    /// An optional implementation of the <see cref="IArchiveReaderDelegate"/>
    /// interface which is used to report the progress or errors during an extract operation.
    /// </param>
    /// <param name="config">
    /// Optional configuration parameters for this <see cref="ArchiveReader"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="stream"/> is <c>null</c>.</exception>
    /// <exception cref="IOException">Thrown if the archive-file could not be opened.</exception>
    /// <exception cref="NotSupportedException">Thrown if the archive format could not be determined.</exception>
    /// <exception cref="BadImageFormatException">Thrown if the native library could not be loaded.</exception>
    /// <exception cref="DllNotFoundException">Thrown if the native library could not be found.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the archive open operation failed.</exception>
    public ArchiveReader(Stream stream, bool leaveOpen, IArchiveReaderDelegate @delegate = null, ArchiveConfig config = null)
    {
        Config = ArchiveConfig.CloneOrDefault(config);

        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        _delegate = @delegate ?? ArchiveReaderDelegate.Default;
        _format = Detail.Format.DetectAndRestoreStreamPosition(stream, out _offset, out _);
        _libraryHandle = Native.LoadLibrary(Config.NativeLibraryPath);
        _reader = CreateArchiveReader();
        _stream = new ArchiveStream(stream, leaveOpen);
        _entries = ArchiveEntryList.Read(_reader, _stream.BaseStream, out var uncompressedArchiveSize);

        UncompressedArchiveSize = uncompressedArchiveSize;
    }

    /// <summary>
    /// Gets the size the entire archive content requires when being extracted,
    /// in bytes.
    /// </summary>
    public ulong UncompressedArchiveSize { get; }

    /// <summary>
    /// Gets the number of entries this archive has.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if this object has already been disposed.
    /// </exception>
    public long Count
    {
        get
        {
            EnsureNotDisposed();
            return _entries.Length;
        }
    }

    /// <summary>
    /// Gets the archive format.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if this object has already been disposed.
    /// </exception>
    public ArchiveFormat Format
    {
        get
        {
            EnsureNotDisposed();
            return _format;
        }
    }

    /// <summary>
    /// Gets a list of all entries in this archive.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if this object has already been disposed.
    /// </exception>
    public IReadOnlyList<ArchiveEntry> Entries
    {
        get
        {
            EnsureNotDisposed();
            return _entries;
        }
    }

    /// <summary>
    /// Performs a kind of dry-run for all entries to test if they can be extracted.
    /// </summary>
    /// <returns>
    /// <c>true</c> if all entries can be extracted.
    /// </returns>
    /// <exception cref="ArchiveOperationException">
    /// Thrown if <see cref="ArchiveConfig.IgnoreOperationErrors"/> is set to <c>false</c> and an
    /// <see cref="OperationResult"/> other than <see cref="OperationResult.Ok"/> is reported by
    /// the native library.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if this object has already been disposed.
    /// </exception>
    public bool CanExtractEntries()
    {
        EnsureNotDisposed();

        using var guard = new OpenArchiveGuard(_reader, _stream.BaseStream);
        using var context = new ExtractContext(this);

        guard.EnsureOpened();

        return 0 == _reader.Extract(null, uint.MaxValue, 1, context);
    }

    /// <summary>
    /// Extracts all entries referenced in <paramref name="indices"/>.
    /// </summary>
    /// <param name="indices">
    /// An <see cref="IndexList"/> containing the indices of the entries to extract, or
    /// <see cref="IndexList.All"/> to extract all entries.
    /// </param>
    /// <param name="onGetStream">
    /// A callback which is invoked to obtain the output stream for
    /// an archive entry. The stream is then used to write the uncompressed content.
    /// </param>
    /// <param name="flags">
    /// Additional flags to configure the extraction behavior. See
    /// <see cref="ArchiveFlags"/> for details.
    /// </param>
    /// 
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="targetDir"/> is <c>null</c> or empty.</exception>
    /// <exception cref="IOException">
    /// Thrown if the given <paramref name="targetDir"/> cannot be created in case it does not yet exist.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown anything else failed during extraction
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the object has already been disposed.
    /// </exception>
    /// <exception cref="ArchiveOperationException">
    /// Thrown if <see cref="ArchiveConfig.IgnoreOperationErrors"/> is set to <c>false</c> and an
    /// <see cref="OperationResult"/> other than <see cref="OperationResult.Ok"/> is reported by
    /// the native library.
    /// </exception>
    /// <exception cref="Exception">
    /// A more generic exception may be thrown when the native library reports an error.
    /// The <see cref="Exception.HResult"/> property may be used to retrieve the error code.
    /// </exception>
    public void Extract(IndexList indices, Func<ArchiveEntry, Stream> onGetStream, ArchiveFlags flags = ArchiveFlags.None)
    {
        EnsureArgumentNotNull(indices, nameof(indices));
        EnsureArgumentNotNull(onGetStream, nameof(onGetStream));
        EnsureEntryIndicesAreValid(indices.Values);

        using var guard = new OpenArchiveGuard(_reader, _stream.BaseStream);
        using var context = new ExtractContext(this, _delegate, flags, onGetStream);
        
        guard.EnsureOpened();

        var result = _reader.Extract(indices.Values, indices.Length, 0, context);

        Marshal.ThrowExceptionForHR(result);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _stream.Dispose();
    }

    /// <summary>
    /// Gets the archive configuration.
    /// </summary>
    internal ArchiveConfig Config { get; }

    /// <summary>
    /// Opens the archive for a transaction, allowing multiple <c>Extract</c> calls
    /// without the need to create a context or to open the archive per call.
    /// </summary>
    /// <param name="onGetStream">
    /// A callback which is invoked to obtain the output stream for
    /// an archive entry. The stream is then used to write the uncompressed content.
    /// </param>
    /// <param name="flags">
    /// Additional flags to configure the extraction behavior. See <see cref="ArchiveFlags"/>
    /// for details.
    /// </param>
    /// <returns>
    /// An instance of the <see cref="OpenArchiveGuard"/> which must be disposed
    /// once the transaction is complete.
    /// </returns>
    internal (OpenArchiveGuard, ExtractContext) CreateTransaction(Func<ArchiveEntry, Stream> onGetStream, ArchiveFlags flags) => 
        (new OpenArchiveGuard(_reader, _stream.BaseStream), new ExtractContext(this, _delegate, flags, onGetStream));

    /// <summary>
    /// Extracts the given <paramref name="indices"/> indices to the streams provided
    /// by the <paramref name="context"/>.
    /// 
    /// This method is used by the <see cref="ExtractTransaction"/>, which allows
    /// extraction of multiple files with separate <c>Extract</c> calls without
    /// the need to reopen the archive for each call.
    /// </summary>
    /// <param name="indices">
    /// An array containing the indices of the entries to extract.
    /// </param>
    /// <param name="length">
    /// The number of entries to extract.
    /// </param>
    /// <param name="context">
    /// The context to use to obtain streams and track the progress.
    /// </param>
    /// <exception cref="ArgumentException">Thrown if any index is out of range.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the object has already been disposed.</exception>
    /// <exception cref="Exception">
    /// A more generic exception may be thrown when the native library reports an error.
    /// The <see cref="Exception.HResult"/> property may be used to retrieve the error code.
    /// </exception>
    internal void Extract(uint[] indices, int length, ExtractContext context)
    {
        EnsureEntryIndicesAreValid(indices);
        EnsureNotDisposed();

        var result = _reader.Extract(indices, (uint)length, 0, context);

        Marshal.ThrowExceptionForHR(result);
    }

    private IArchiveReader CreateArchiveReader()
    {
        return ComObjectFactory.CreateObject<IArchiveReader>(_libraryHandle, _format);
    }

    private void EnsureNotDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ArchiveWriter), "The object has already been disposed.");
        }
    }

    private void EnsureEntryIndicesAreValid(uint[] indices)
    {
        if (indices == null)
        {
            return;
        }

        if (indices.Any(index => index >= Count))
        {
            throw new ArgumentException("One or more of the given indices are out of range.");
        }
    }

    private void EnsureArgumentNotNull<T>(T argument, string name) where T : class
    {
        if (argument == null)
        {
            throw new ArgumentNullException(name);
        }
    }
}