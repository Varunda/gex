using System;

namespace SevenZip.Detail;

/// <summary>
/// A class storing the desired properties of a file. Typically used to provide
/// the timestamps to apply to an extracted file once the extraction is done.
/// </summary>
internal sealed class FileUpdateInfo
{
    /// <summary>
    /// Gets the creation time.
    /// </summary>
    public DateTime CreationTime { get; init; }

    /// <summary>
    /// Gets the last access time.
    /// </summary>
    public DateTime LastAccessTime { get; init; }

    /// <summary>
    /// Gets the last write time.
    /// </summary>
    public DateTime LastWriteTime { get; init; }
}