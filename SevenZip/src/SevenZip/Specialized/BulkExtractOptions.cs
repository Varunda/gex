using System;

namespace SevenZip.Specialized;

/// <summary>
/// The <see cref="BulkExtractOptions"/> can be used to configure the behavior
/// of the <see cref="ArchiveReaderExtensions.BulkExtract"/> extension method.
/// </summary>
public sealed class BulkExtractOptions
{
    /// <summary>
    /// Gets the predicate to determine whether an <see cref="ArchiveEntry"/>
    /// shall be extracted. If set to <c>null</c>, all entries will be
    /// extracted.
    /// </summary>
    public Func<ArchiveEntry, bool> Predicate { get; init; }

    /// <summary>
    /// Gets a value indicating whether the stream pool used during extraction
    /// shall use native or managed memory.
    /// </summary>
    public bool UseNativeStreamPool { get; init; }

    /// <summary>
    /// Gets a value indicating whether the <see cref="IDisposable.Dispose"/>
    /// method of each stream should be invoked after it has been consumed.
    /// </summary>
    public bool DisposeArchiveEntryStreamAfterUse { get; init; }

#if NET6_0_OR_GREATER
    /// <summary>
    /// Gets a value indicating if entries exceeding the .NET array size limit of 2147483591
    /// will be written to a temporary file stream instead of an in-memory
    /// stream. 
    /// Otherwise, the <see cref="InMemoryArchiveRepacker"/> throws an
    /// <see cref="InvalidOperationException"/> exception for oversized entries.
    /// </summary>
    /// <remarks>
    /// When using this flag, <see cref="DisposeArchiveEntryStreamAfterUse"/> should be
    /// specified as well to ensure that the temporary file streams are
    /// deleted and do not pollute the disk.
    /// </remarks>
    public bool UseFileStreamForOversizedEntries { get; init; }
#endif

    /// <summary>
    /// Gets the threshold, in bytes, indicating when to throw an exception during 
    /// a bulk extract operation for oversized entries. If not specified, 
    /// <see cref="InMemoryArchiveRepacker.MaxCapacity"/> will be used, which
    /// provides the maximum size for in-memory streams in .NET.
    /// 
    /// IF <see cref="UseFileStreamForOversizedEntries"/> is set to <c>true</c>,
    /// temporary files will be used to extract entries exceeding this threshold
    /// and no exception will be thrown.
    /// </summary>
    public int? OversizedEntryThreshold { get; init; }

    /// <summary>
    /// Gets the capacity for the internal stream pool. If set to zero, the
    /// capacity will be determined automatically based on the uncompressed
    /// archive size.
    /// </summary>
    public int StreamPoolCapacity { get; init; }
}
