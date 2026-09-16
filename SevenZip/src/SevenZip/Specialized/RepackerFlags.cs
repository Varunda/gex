using System;

namespace SevenZip.Specialized;

/// <summary>
/// Flags to configure the behavior of the <see cref="InMemoryArchiveRepacker"/> class.
/// </summary>
[Flags]
public enum RepackerFlags
{
    /// <summary>
    /// Specifies the default behavior.
    /// </summary>
    None = 0,

    /// <summary>
    /// Use a stream pool that refers to native memory. This may be useful for
    /// larger archives to relieve the garbage collector.
    /// </summary>
    UseNativeMemory = 1 << 0,

    /// <summary>
    /// If specified, the <see cref="InMemoryArchiveRepacker"/> will call the
    /// <see cref="IDisposable.Dispose"/> method of each stream after it has 
    /// been rewritten.
    /// </summary>
    DisposeArchiveEntryStreamAfterUse = 1 << 1,

#if NET6_0_OR_GREATER
    /// <summary>
    /// If specified, entries exceeding the .NET array size limit of 2147483591
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
    UseFileStreamForOversizedArchiveEntries = 1 << 2,
#endif
}
