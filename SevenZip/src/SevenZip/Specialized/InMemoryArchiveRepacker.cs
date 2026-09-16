using System;
using System.Collections.Generic;
using System.IO;

namespace SevenZip.Specialized;

/// <summary>
/// The <see cref="InMemoryArchiveRepacker"/> allows to transfer files from one archive to another
/// by storing the extracted data in memory. This requires huge amount of heap memory for larger
/// archives, but may be more efficient than storing the entire archive to disk first.
/// 
/// Whether there is a performance gain varies depending on the use case and should be measured for
/// each case individually.
/// </summary>
public sealed class InMemoryArchiveRepacker
{
    private readonly ArchiveReader _reader;
    private readonly ArchiveWriter _writer;
    private readonly RepackerFlags _flags;
    private readonly int _capacity;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryArchiveRepacker"/> class.
    /// </summary>
    /// <param name="reader">
    /// The archive to read data from.
    /// </param>
    /// <param name="writer">
    /// The archive to write the extracted data to. This can either be a new or an existing
    /// archive.
    /// </param>
    /// <param name="flags">
    /// Optional flags to configure the repacker. See <see cref="RepackerFlags"/> for details.
    /// </param>
    /// <param name="capacity">
    /// The maximum capacity that can be used to store the extracted data. Once this limit
    /// is reached, the yet extracted data will be written to the target archive and the
    /// memory will be reused for the next bulk of streams.
    /// If set to zero, the capacity will be determined based on the uncompressed archive
    /// size.
    /// </param>
    /// <param name="oversizedEntryThreshold">
    /// Can be used to override the default threshold indicating when an archive entry
    /// is too large to be stored in memory. If not specified, <see cref="MaxCapacity"/>
    /// will be used - the maximum size for in-memory streams in .NET.
    /// 
    /// See <see cref="OversizedEntryThreshold"/> for details.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="oversizedEntryThreshold"/> is negative.
    /// </exception>
    public InMemoryArchiveRepacker(ArchiveReader reader, ArchiveWriter writer, 
        RepackerFlags flags = RepackerFlags.None,
        int capacity = 0,
        int? oversizedEntryThreshold = null)
    {
        if (oversizedEntryThreshold is { } value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(oversizedEntryThreshold),
                    $"{nameof(oversizedEntryThreshold)} must not be negative.");
            }

            OversizedEntryThreshold = (ulong)value;
        }
        else
        {
            OversizedEntryThreshold = MaxCapacity;
        }

        _reader = reader;
        _writer = writer;
        _flags = flags;
        _capacity = capacity;
    }

    /// <summary>
    /// Gets the maximum capacity for the internal memory reserved for the streams.
    /// </summary>
    public const int MaxCapacity = ScopedStreamPoolFactory.MaxCapacity;

    /// <summary>
    /// Gets the threshold, in bytes, indicating when to throw an exception during 
    /// a repack operation for oversized entries. If not specified, 
    /// <see cref="MaxCapacity"/> will be used, which provides the maximum size
    /// for in-memory streams in .NET.
    /// 
    /// IF <see cref="RepackerFlags.UseFileStreamForOversizedArchiveEntries"/> is set,
    /// temporary files will be used to extract entries exceeding this threshold
    /// and no exception will be thrown.
    /// </summary>
    public ulong OversizedEntryThreshold { get; }

    /// <summary>
    /// Starts the repacking process and copies files from the source archive to the
    /// target archive.
    /// </summary>
    /// <param name="predicate">
    /// A custom filter that can be used to determine the entries to copy. If this filter
    /// returns <see langword="null"/> or an empty string, it will be skipped.
    /// 
    /// The callback is also invoked for directory entries.
    /// </param>
    /// <param name="getArchivePath">
    /// A custom selector to transform the archive paths.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the uncompressed size of an entry within the archive exceeds
    /// <see cref="MaxCapacity"/>.
    /// </exception>
    public void Repack(Predicate<ArchiveEntry> predicate, Func<ArchiveEntry, string> getArchivePath)
    {
        predicate ??= _ => true;

        var flags = ArchiveFlags.None;
        var indices = new Dictionary<int, (string, Stream)>();
        var useNativeStreamPool = (_flags & RepackerFlags.UseNativeMemory) != 0;

        using var pool = ScopedStreamPoolFactory.Create(_reader, useNativeStreamPool, _capacity);
        using var transaction = new ExtractTransaction(_reader, flags);

        foreach (var entry in _reader.Entries)
        {
            if (predicate(entry) == false)
            {
                continue;
            }

            var path = getArchivePath(entry);
            var size = (int)entry.UncompressedSize;

            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            if (entry.IsDirectory)
            {
                _writer.AddDirectory(path);
                continue;
            }

            if (entry.UncompressedSize > OversizedEntryThreshold)
            {
#if NET6_0_OR_GREATER
                var useFileStreamForLargeEntries = (_flags & RepackerFlags.UseFileStreamForOversizedArchiveEntries) != 0;
                if (useFileStreamForLargeEntries)
                {
                    var preallocationSize = (long)entry.UncompressedSize;
                    var stream = SevenZip.Detail.TempFile.Create(
                        _reader.Config, deleteOnClose: true, preallocationSize);

                    indices.Add(entry.Index, (path, stream));
                    continue;
                }
#endif
                throw new InvalidOperationException($"The entry with '{entry.Path}' exceeds the stream capacity.");
            }

            if (size > pool.Capacity)
            {
                indices.Add(entry.Index, (path, new MemoryStream(size)));
                continue;
            }
 
            if (pool.CanCreateStreamWithCapacity(size) == false)
            {
                RepackPendingEntries(transaction, indices, pool);
            }

            indices.Add(entry.Index, (path, pool.CreateStream(size)));
        }

        RepackPendingEntries(transaction, indices, pool);
    }

    private void RepackPendingEntries(ExtractTransaction transaction, Dictionary<int, (string, Stream)> indices, IScopedStreamPool pool)
    {
        if (indices.Count == 0)
        {
            return;
        }

        transaction.Extract(indices.Keys, index => indices[index].Item2);

        foreach (var (archivePath, stream) in indices.Values)
        {
            stream.Seek(0, SeekOrigin.Begin);

            _writer.AddFile(archivePath, stream);
        }

        _writer.Compress();

        if ((_flags & RepackerFlags.DisposeArchiveEntryStreamAfterUse) != 0)
        {
            foreach (var (_, stream) in indices.Values)
            {
                stream.Dispose();
            }
        }

        indices.Clear();
        pool.Discard();
    }
}