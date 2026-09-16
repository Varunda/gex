using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using SevenZip.Detail;
using SevenZip.Interop;

namespace SevenZip;

/// <summary>
/// The <see cref="ArchiveWriter"/> can be used to update existing archives or to create new ones.
/// It makes use of the native <c>7z</c> library, which is loaded when creating an instance
/// of this class.
/// </summary>
public sealed class ArchiveWriter : IDisposable
{
    private readonly IArchiveWriterDelegate _delegate;
    private readonly IntPtr _libraryHandle;
    private readonly ArchiveFormat _format;
    private readonly ArchiveStream _stream;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveWriter"/> class which can be used to 
    /// modify an existing archive. 
    /// 
    /// When modifying the archive, the entire content has to be stored in a temporary
    /// file first, which may take some time for larger archives. The existing file or memory will then be
    /// overwritten when calling <see cref="Compress(CompressProperties)"/>.
    /// </summary>
    /// <param name="path">
    /// The path to a file to write the archive data to.
    /// </param>
    /// <param name="delegate">
    /// An optional instance of the <see cref="IArchiveWriterDelegate"/> interface which can be used
    /// to track errors and the progress of the compress operation.
    /// </param>
    /// <param name="config">
    /// An optional instance of the <see cref="ArchiveConfig"/> class which can be used to specify
    /// the location of the native 7z library or a password for the archive..
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when the referenced file contains less than 16 bytes.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown if the format of an existing archive could not be determined.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Thrown if the native 7z library could not be found.
    /// </exception>
    /// <exception cref="BadImageFormatException">
    /// Thrown if the native 7z library could not be loaded.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown when accessing an IO stream fails.
    /// </exception>
    public ArchiveWriter(string path, IArchiveWriterDelegate @delegate = null, ArchiveConfig config = null)
        : this(null, File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite), leaveOpen: false, @delegate, config)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveWriter"/> class which can be used to create a new
    /// archive at the given <paramref name="path"/>. If the file already exists, it will be overwritten.
    /// </summary>
    /// <param name="format">
    /// The desired format for the new archive.
    /// </param>
    /// <param name="path">
    /// The path to a file to write the archive data to.
    /// </param>
    /// <param name="delegate">
    /// An optional instance of the <see cref="IArchiveWriterDelegate"/> interface which can be used
    /// to track errors and the progress of the compress operation.
    /// </param>
    /// <param name="config">
    /// An optional instance of the <see cref="ArchiveConfig"/> class which can be used to specify
    /// the location of the native 7z library or a password for the archive..
    /// </param>
    /// <exception cref="NotSupportedException">
    /// Thrown if the format of an existing archive could not be determined.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Thrown if the native 7z library could not be found.
    /// </exception>
    /// <exception cref="BadImageFormatException">
    /// Thrown if the native 7z library could not be loaded.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown when accessing an IO stream fails.
    /// </exception>
    public ArchiveWriter(ArchiveFormat format, string path, IArchiveWriterDelegate @delegate = null, ArchiveConfig config = null)
        : this(format, File.Open(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite), leaveOpen: false, @delegate, config)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveWriter"/> class which can be used to modify
    /// an existing archive.
    ///
    /// When modifying the archive, the entire content has to be stored in a temporary
    /// file first, which may take some time for larger archives. The existing file or memory will then be
    /// overwritten when calling <see cref="Compress(CompressProperties)"/>.
    /// </summary>
    /// <param name="stream">
    /// The stream to read existing archive data from to write the new data to. The <see cref="SevenZip.ArchiveFormat"/>
    /// is read from this stream.
    /// </param>
    /// <param name="leaveOpen">
    /// Specifies whether the given <paramref name="leaveOpen"/> shall be disposed when this instance
    /// is being disposed.
    /// </param>
    /// <param name="delegate">
    /// An optional instance of the <see cref="IArchiveWriterDelegate"/> interface which can be used
    /// to track errors and the progress of the compress operation.
    /// </param>
    /// <param name="config">
    /// An optional instance of the <see cref="ArchiveConfig"/> class which can be used to specify
    /// the location of the native 7z library or a password for the archive..
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="stream"/> is <c>null.</c>
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the given <paramref name="stream"/> does not meet the requirements. For new (or overridden)
    /// archives, the stream must support seek and write operations. For existing archives that are modified,
    /// it must additionally support read operations.
    ///   
    /// For existing archives, this method is also thrown when the stream contains less than 16 bytes.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown if the format of an existing archive could not be determined.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Thrown if the native 7z library could not be found.
    /// </exception>
    /// <exception cref="BadImageFormatException">
    /// Thrown if the native 7z library could not be loaded.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="stream"/> refers to an existing archive but the 7z library
    /// failed to open it.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown when accessing the <paramref name="stream"/> fails.
    /// </exception>
    public ArchiveWriter(Stream stream, bool leaveOpen, IArchiveWriterDelegate @delegate = null, ArchiveConfig config = null)
        : this(null, stream, leaveOpen, @delegate, config)
    { 
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveWriter"/> class which can be used to
    /// create a new archive.
    ///     
    /// This constructor ignores any data that might be present in the given <paramref name="stream"/>
    /// and creates a new archive with the specified <paramref name="format"/>.
    /// </summary>
    /// <param name="format">
    /// The desired format for the new archive.
    /// </param>
    /// <param name="stream">
    /// The target stream to write the data to.
    ///     
    /// In any case the contents of the <paramref name="stream"/> will be overwritten when calling
    /// <see cref="Compress(CompressProperties)"/> to create the archive.
    /// </param>
    /// <param name="leaveOpen">
    /// Specifies whether the given <paramref name="leaveOpen"/> shall be disposed when this instance
    /// is being disposed.
    /// </param>
    /// <param name="delegate">
    /// An optional instance of the <see cref="IArchiveWriterDelegate"/> interface which can be used
    /// to track errors and the progress of the compress operation.
    /// </param>
    /// <param name="config">
    /// An optional instance of the <see cref="ArchiveConfig"/> class which can be used to specify
    /// the location of the native 7z library or a password for the archive..
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="stream"/> is <c>null.</c>
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the given <paramref name="stream"/> does not meet the requirements. For new (or overridden)
    /// archives, the stream must support seek and write operations. For existing archives that are modified,
    /// it must additionally support read operations.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown if the format of an existing archive could not be determined.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Thrown if the native 7z library could not be found.
    /// </exception>
    /// <exception cref="BadImageFormatException">
    /// Thrown if the native 7z library could not be loaded.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="stream"/> refers to an existing archive but the 7z library
    /// failed to open it.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown when accessing the <paramref name="stream"/> fails.
    /// </exception>
    public ArchiveWriter(ArchiveFormat format, Stream stream, bool leaveOpen, IArchiveWriterDelegate @delegate = null, ArchiveConfig config = null)
        : this(new ArchiveFormat?(format), stream, leaveOpen, @delegate, config)
    { 
    }

    private ArchiveWriter(ArchiveFormat? format, Stream stream, bool leaveOpen, IArchiveWriterDelegate @delegate, ArchiveConfig config)
    {
        Config = ArchiveConfig.CloneOrDefault(config);

        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream), "The given stream must not be null.");
        }

        if (format != null)
        {
            if (!(stream.CanSeek && stream.CanWrite))
            {
                throw new ArgumentException("The given stream must support seek and write operations.", nameof(stream));
            }

            _format = format.Value;

            // If a format is specified a new archive shall be created. Therefore, we reset
            // the stream here.
            stream.Position = 0L;
            stream.SetLength(0);
        }
        else
        {
            if (!(stream.CanSeek && stream.CanRead && stream.CanWrite))
            {
                throw new ArgumentException("The given stream must support seek, write and read operations.", nameof(stream));
            }

            _format = Format.DetectAndRestoreStreamPosition(stream, out _, out _);
        }

        _libraryHandle = Native.LoadLibrary(Config.NativeLibraryPath);
        _delegate = @delegate ?? ArchiveWriterDelegate.Default;
        _stream = new ArchiveStream(stream, leaveOpen);

        UpdateContext = new ArchiveUpdateContext(ReadExistingArchiveEntryList(stream));
    }

    /// <summary>
    /// Gets the size the entire archive content requires when being extracted,
    /// in bytes.
    /// This property only includes the archive entries that are already part
    /// of the current archive. It does not consider any pending entries.
    /// </summary>
    /// <remarks>
    /// This property is update after a call to <see cref="Compress(CompressProperties)"/>.
    /// </remarks>
    public ulong UncompressedArchiveSize => UpdateContext.UncompressedArchiveSize;

    /// <summary>
    /// Gets the selected format for this archive.
    /// </summary>
    public ArchiveFormat ArchiveFormat => _format;

    /// <summary>
    /// Gets a list of all already existing archive entries.
    /// 
    /// <para>
    /// This list is being updated after a call to <see cref="Compress(CompressProperties)"/>.
    /// </para>
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if this object has already been disposed.
    /// </exception>
    public IReadOnlyList<ArchiveEntry> ExistingEntries
    {
        get
        {
            EnsureNotDisposed();
            return UpdateContext.ArchiveEntries;
        }
    }

    /// <summary>
    /// Adds an empty directory to the archive.
    /// </summary>
    /// <param name="archivePath">The path within the archive.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if any of the arguments is <c>null</c>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if this object has already been disposed.
    /// </exception>
    public void AddDirectory(string archivePath)
    {
        EnsureNotDisposed();
        UpdateContext.AddDirectory(archivePath);
    }

    /// <summary>
    /// Adds a file to the update context of this archive. The data will actually be written
    /// when calling <see cref="Compress(CompressProperties)"/>.
    /// </summary>
    /// <param name="archivePath">
    /// The path the file shall have within the archive. This path
    /// may contain directories, where each directory has to be separated with the '/'
    /// character.
    /// </param>
    /// <param name="path">
    /// The path of the file whose content shall be added.
    /// </param>
    /// <returns>
    /// The temporary index for the new entry. This index is only valid
    /// until <see cref="Compress(CompressProperties)"/> is called. Afterwards,
    /// the index of the file might be different.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if any of the arguments is <c>null</c>.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Thrown if <paramref name="path"/> does not refer to an existing file.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if this object has already been disposed.
    /// </exception>
    public int AddFile(string archivePath, string path)
    {
        EnsureNotDisposed();

        return UpdateContext.Add(archivePath, path);
    }

    /// <summary>
    /// Adds the contents of the given <paramref name="stream"/> to the update context
    /// of this archive. The data will actually be written
    /// when calling <see cref="Compress(CompressProperties)"/>.
    /// </summary>
    /// <param name="archivePath">
    /// The path the file shall have within the archive. This path
    /// may contain directories, where each directory has to be separated with the '/'
    /// character.
    /// </param>
    /// <param name="stream">
    /// The stream providing the contents to add. This stream must support read operations.
    /// </param>
    /// <param name="leaveOpen">
    /// Specifies whether the stream shall be closed once <see cref="Compress(CompressProperties)"/>
    /// has been called.
    /// </param>
    /// <returns>
    /// The temporary index for the new entry. This index is only valid
    /// until <see cref="Compress(CompressProperties)"/> is called. Afterwards,
    /// the index of the file might be different.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if any of the arguments is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if the given stream does not support read operations.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown if the given stream cannot be accessed.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if this object has already been disposed.
    /// </exception>
    public int AddFile(string archivePath, Stream stream, bool leaveOpen = false)
    {
        EnsureNotDisposed();

        if (stream.CanRead == false)
        {
            throw new ArgumentException($"The given stream must support read operations.");
        }

        return UpdateContext.Add(archivePath, stream, leaveOpen);
    }

    /// <summary>
    /// Adds the given index of an existing archive entry to the update context
    /// and marks it for deletion. The entry will be removed from the archive
    /// when <see cref="Compress(CompressProperties)"/> is called.
    /// </summary>
    /// <param name="index">
    /// A valid index of an existing archive entry.
    /// </param>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown if <paramref name="index"/> is not within the valid range.
    /// </exception>
    public void DeleteEntry(int index)
    {
        EnsureNotDisposed();
        UpdateContext.Delete(index);
    }

    /// <summary>
    /// Adds the given index and an existing file to the update context and
    /// marks it for replacement. The entry will be overridden when
    /// <see cref="Compress(CompressProperties)"/> is called.
    /// </summary>
    /// <param name="index">
    /// The index of an existing archive entry whose content shall be replaced.
    /// </param>
    /// <param name="path">
    /// The path of the file whose content shall be added.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="path"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Thrown if <paramref name="path"/> does not refer to an existing file.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if this object has already been disposed.
    /// </exception>
    public void ReplaceEntry(int index, string path)
    {
        EnsureNotDisposed();
        UpdateContext.Replace(index, path);
    }

    /// <summary>
    /// Adds the given index and stream to the update context and
    /// marks it for replacement. The entry will be overridden when
    /// <see cref="Compress(CompressProperties)"/> is called.
    /// </summary>
    /// <param name="index">
    /// The index of an existing archive entry whose content shall be replaced.
    /// </param>
    /// <param name="stream">
    /// The stream providing the contents to add. This stream must support read operations.
    /// </param>
    /// <param name="leaveOpen">
    /// Specifies whether the stream shall be closed once <see cref="Compress(CompressProperties)"/>
    /// has been called.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if any of the arguments is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if the given stream does not support read operations.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown if the given stream cannot be accessed.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if this object has already been disposed.
    /// </exception>
    public void ReplaceEntry(int index, Stream stream, bool leaveOpen = false)
    {
        EnsureNotDisposed();
        UpdateContext.Replace(index, stream, leaveOpen);
    }

    /// <summary>
    /// Writes all new and modified entries to the archive and removes entries that were
    /// marked for removal, if operating on an existing archive.
    /// 
    /// When operating on an existing archive, the current content has to be stored in
    /// a temporary file first since simultaneous read and write operations are not
    /// supported.
    /// 
    /// After the changes have been written, the internal state and the <see cref="ExistingEntries"/>
    /// properties are updated to reflect the current archive contents.
    /// When the associated stream does not support read operations, this method must only
    /// be called once. Otherwise, an exception will be thrown.
    /// </summary>
    /// <param name="properties">
    /// An optional instance of the <see cref="CompressProperties"/> class which can
    /// be used to configure the compress operation.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the associated stream refers to an existing archive but does not support
    /// read operations.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if this object has already been disposed.
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
    public void Compress(CompressProperties properties = null)
    {
        EnsureNotDisposed();

        if (_stream.BaseStream.Length > 0 && !_stream.BaseStream.CanRead)
        {
            throw new InvalidOperationException($"{nameof(Compress)} cannot be called multiple times for non-readable streams.");
        }

        var intermediatePath = string.Empty;
        var input = default(ArchiveStream);
        try
        {
            if (UpdateContext.IsEmptyArchive is false)
            {
                intermediatePath = Config.GetTempFileName();
                input = new ArchiveStream(
                    File.Open(intermediatePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite),
                    leaveOpen: false);

                _stream.BaseStream.Seek(0, SeekOrigin.Begin);
                _stream.BaseStream.CopyTo(input.BaseStream);

                input.BaseStream.Seek(0, SeekOrigin.Begin);
            }

            using (var context = new CompressContext(this, _delegate))
            {
                _stream.BaseStream.Seek(0, SeekOrigin.Begin);

                var count = (uint)UpdateContext.NewArchiveEntryCount;
                var writer = ApplyArchiveWriterProperties(CreateArchiveWriter(input), properties);
                var result = writer.UpdateItems(_stream, count, context);

                Marshal.ThrowExceptionForHR(result);
            }

            UpdateContext.Reset(ReadExistingArchiveEntryList(_stream.BaseStream));
        }
        finally
        {
            input?.Dispose();

            if (string.IsNullOrEmpty(intermediatePath) is false)
            {
                File.Delete(intermediatePath);
            }
        }
    }

    /// <inheritdoc />
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
    /// Gets the update context used to maintain the archive state.
    /// </summary>
    internal ArchiveUpdateContext UpdateContext { get; }

    private IArchiveWriter CreateArchiveWriter(IInputStream existingArchiveStream)
    {
        if (existingArchiveStream == null)
        {
            return ComObjectFactory.CreateObject<IArchiveWriter>(_libraryHandle, _format);
        }

        var reader = ComObjectFactory.CreateObject<IArchiveReader>(_libraryHandle, _format);
        var offset = (ulong)(1 << 15);

        reader.Open(existingArchiveStream, ref offset, callback: null);

        return ComCast.As<IArchiveReader, IArchiveWriter>(reader);
    }

    private IArchiveWriter ApplyArchiveWriterProperties(IArchiveWriter writer, CompressProperties properties)
    {
        properties?.Apply(writer, _format);

        return writer;
    }

    private ArchiveEntryList ReadExistingArchiveEntryList(Stream stream)
    {
        var entryList = default(ArchiveEntryList);

        if (stream.Length > 0 && stream.CanRead)
        {
            var position = stream.Position;
            var reader = ComObjectFactory.CreateObject<IArchiveReader>(_libraryHandle, _format);

            entryList = new ArchiveEntryList(reader, stream);
            stream.Position = position;
        }

        return entryList;
    }

    private void EnsureNotDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ArchiveWriter), "The object has already been disposed.");
        }
    }
}