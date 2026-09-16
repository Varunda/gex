using System;
using System.Diagnostics;
using System.IO;

namespace SevenZip;

/// <summary>
/// Representation of a single entry within an archive, which is either
/// a file or a directory.
/// </summary>
[DebuggerDisplay("ArchiveEntry(Index = {Index}, Path = {Path}, Attributes = {Attributes}")]
public readonly struct ArchiveEntry
{
    /// <summary>
    /// Gets the entry index within the archive. This value must be used to explicitly
    /// extract this file or directory.
    /// </summary>
    public int Index { get; init; }
        
    /// <summary>
    /// Gets the relative path of the entry.
    /// </summary>
    public string Path { get; init; }

    /// <summary>
    /// Gets the creation timestamp for the entry.
    /// </summary>
    public DateTime CreationTime { get; init; }

    /// <summary>
    /// Gets the last write timestamp for the entry.
    /// </summary>
    public DateTime LastWriteTime { get; init; }
        
    /// <summary>
    /// Gets the last access timestamp for the entry.
    /// </summary>
    public DateTime LastAccessTime { get; init; }
        
    /// <summary>
    /// Gets the uncompressed size of the entry.
    /// </summary>
    public ulong UncompressedSize { get; init; }
        
    /// <summary>
    /// Gets the CRC of the entry. This property might be zero, depending on the archive format.
    /// </summary>
    public uint Crc { get; init; }
        
    /// <summary>
    /// Gets additional attributes for the entry.
    /// </summary>
    public FileAttributes Attributes { get; init; }
        
    /// <summary>
    /// Gets a value indicating if the entry is a directory.
    /// </summary>
    public bool IsDirectory { get; init; }
        
    /// <summary>
    /// Gets a value indicating if the content is encrypted.
    /// </summary>
    public bool Encrypted { get; init; }
        
    /// <summary>
    /// Gets an optional comment.
    /// </summary>
    public string Comment { get; init; }
        
    /// <summary>
    /// Gets the compression method.
    /// </summary>
    public string Method { get; init; }
}