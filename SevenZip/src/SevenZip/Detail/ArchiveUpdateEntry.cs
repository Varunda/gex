using System;
using System.IO;
using SevenZip.Interop;

namespace SevenZip.Detail;

/// <summary>
/// This class represents a new entry to add to an archive or an existing entry
/// that is about to be modified or deleted.
/// </summary>
internal sealed class ArchiveUpdateEntry
{
    private readonly bool _leaveOpen;
    private readonly string _path;
    private Stream _stream;

    /// <summary>
    /// Gets the operation to perform for this entry. See <see cref="ArchiveUpdateOperation"/> for details.
    /// </summary>
    public ArchiveUpdateOperation UpdateOperation { get; }

    /// <summary>
    /// Gets the file attributes of this entry.
    /// </summary>
    public FileAttributes Attributes { get; }

    /// <summary>
    /// Gets the uncompressed size of the content.
    /// </summary>
    public long Size { get; }

    /// <summary>
    /// Gets the path within the archive.
    /// </summary>
    public string ArchivePath { get; }

    /// <summary>
    /// Gets a value indicating whether the entry is a directory or not.
    /// </summary>
    public bool IsDirectory => (Attributes & FileAttributes.Directory) != 0;

    /// <summary>
    /// Gets the last write timestamp for the entry.
    /// </summary>
    public DateTime LastWriteTime { get; }

    /// <summary>
    /// Gets the last access timestamp for the entry.
    /// </summary>
    public DateTime LastAccessTime { get; }

    /// <summary>
    /// Gets the creation timestamp for the entry.
    /// </summary>
    public DateTime CreationTime { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveUpdateEntry"/> class which adds an
    /// empty directory to the archive.
    /// </summary>
    /// <param name="archivePath">The directory path within the archive.</param>
    /// <returns>The initialized <see cref="ArchiveUpdateEntry"/>.</returns>
    public static ArchiveUpdateEntry AddDirectory(string archivePath)
    {
        return new ArchiveUpdateEntry(ArchiveUpdateOperation.Add, archivePath, FileAttributes.Directory);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveUpdateEntry"/> class which adds a
    /// new entry with the contents of the given <paramref name="source"/>.
    /// </summary>
    /// <param name="archivePath">The path the entry should have within the archive.</param>
    /// <param name="source">The stream to read the contents from.</param>
    /// <param name="leaveOpen"><c>true</c> to dispose the given stream once all data has been read. <c>false</c> to leave it open.</param>
    /// <returns>The initialized <see cref="ArchiveUpdateEntry"/>.</returns>
    public static ArchiveUpdateEntry Add(string archivePath, Stream source, bool leaveOpen)
    {
        return new ArchiveUpdateEntry(ArchiveUpdateOperation.Add, archivePath, source, leaveOpen);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveUpdateEntry"/> class which adds a
    /// new entry with the contents of a physical file located at <paramref name="path"/>.
    /// </summary>
    /// <param name="archivePath">The path the entry should have within the archive.</param>
    /// <param name="path">The path to the physical file to add.</param>
    /// <returns>The initialized <see cref="ArchiveUpdateEntry"/>.</returns>
    public static ArchiveUpdateEntry Add(string archivePath, string path)
    {
        return new ArchiveUpdateEntry(ArchiveUpdateOperation.Add, archivePath, path);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveUpdateEntry"/> class which updates an
    /// existing entry with the contents of the given <paramref name="source"/>.
    /// </summary>
    /// <param name="archivePath">The path the entry should have within the archive.</param>
    /// <param name="source">The stream to read the contents from.</param>
    /// <param name="leaveOpen"><c>true</c> to dispose the given stream once all data has been read. <c>false</c> to leave it open.</param>
    /// <returns>The initialized <see cref="ArchiveUpdateEntry"/>.</returns>
    public static ArchiveUpdateEntry Update(string archivePath, Stream source, bool leaveOpen)
    {
        return new ArchiveUpdateEntry(ArchiveUpdateOperation.Update, archivePath, source, leaveOpen);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveUpdateEntry"/> class which updates an
    /// existing entry with the contents of a physical file located at <paramref name="path"/>.
    /// </summary>
    /// <param name="archivePath">The path the entry should have within the archive.</param>
    /// <param name="path">The path to the physical file to add.</param>
    /// <returns>The initialized <see cref="ArchiveUpdateEntry"/>.</returns>
    public static ArchiveUpdateEntry Update(string archivePath, string path)
    {
        return new ArchiveUpdateEntry(ArchiveUpdateOperation.Update, archivePath, path);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveEntry"/> class. This
    /// constructor is used to mark an existing entry for deletion by setting
    /// the <see cref="ArchivePath"/> property to <see cref="string.Empty"/>.
    /// </summary>
    /// <returns>The initialized <see cref="ArchiveUpdateEntry"/>.</returns>
    public static ArchiveUpdateEntry Delete()
    {
        return new ArchiveUpdateEntry(ArchiveUpdateOperation.Delete, archivePath: string.Empty, FileAttributes.Offline);
    }

    /// <summary>
    /// Gets the extension of the entry's filename or <see cref="string.Empty"/>,
    /// if not present.
    /// </summary>
    /// <returns>The file extension or <see cref="string.Empty"/>.</returns>
    public string GetExtension()
    {
        var index = ArchivePath.LastIndexOf(".", StringComparison.InvariantCultureIgnoreCase);
        var isValidIndexPosition = index >= 0 && index + 1 < ArchivePath.Length;

        return isValidIndexPosition ? ArchivePath.Substring(index + 1) : string.Empty;
    }

    /// <summary>
    /// Creates an <see cref="ArchiveStream"/> which can be used to access the
    /// file data to compress.
    /// </summary>
    /// <returns>An <see cref="ArchiveStream"/> to read the content from.</returns>
    public ArchiveStream CreateStream()
    {
        if (TryReleaseStream(out var stream))
        {
            return new ArchiveStream(stream, _leaveOpen);
        }

        if (!string.IsNullOrEmpty(_path))
        {
            return new ArchiveStream(File.Open(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), _leaveOpen);
        }

        throw new InvalidOperationException($"The entry {ArchivePath} does not reference an existing stream or a file.");
    }

    private ArchiveUpdateEntry(ArchiveUpdateOperation operation, string archivePath, FileAttributes attributes)
        : this(path: string.Empty, source: null, leaveOpen: false)
    {
        UpdateOperation = operation;
        Attributes = attributes;
        ArchivePath = archivePath;
        LastAccessTime =
        LastWriteTime =
        CreationTime = DateTime.UtcNow;
    }

    private ArchiveUpdateEntry(ArchiveUpdateOperation operation, string archivePath, string path)
        : this(path, source: null, leaveOpen: false)
    {
        var fileInfo = new FileInfo(path);

        UpdateOperation = operation;
        ArchivePath = archivePath;
        Attributes = fileInfo.Attributes;

        Size = fileInfo.Length;
        LastAccessTime = fileInfo.LastAccessTime;
        LastWriteTime = fileInfo.LastWriteTime;
        CreationTime = fileInfo.CreationTime;
    }

    private ArchiveUpdateEntry(ArchiveUpdateOperation operation, string archivePath, Stream source, bool leaveOpen)
        : this(path: string.Empty, source, leaveOpen)
    {
        UpdateOperation = operation;
        Attributes = FileAttributes.Normal;
        ArchivePath = archivePath;

        Size = source.Length - source.Position;
        LastAccessTime =
        LastWriteTime =
        CreationTime = DateTime.UtcNow;
    }

    private ArchiveUpdateEntry(string path, Stream source, bool leaveOpen) => (_path, _stream, _leaveOpen) = (path, source, leaveOpen);

    private bool TryReleaseStream(out Stream stream)
    {
        stream = _stream;
        _stream = null;
        return stream != null;
    }
}
