namespace SevenZip;

/// <summary>
/// Enumeration listing the names of the supported attributes an archive entry may have.
/// </summary>
/// <remarks>
/// The property values must match the original definition which can be found here:
/// https://github.com/mcmilk/7-Zip/blob/master/CPP/7zip/PropID.h
/// </remarks>
internal enum ArchiveEntryProperty : uint
{
    /// <summary>
    /// The relative path or filename of the archive entry.
    /// </summary>
    Path = 3u,

    /// <summary>
    /// The file extension.
    /// </summary>
    Extension = 5u,

    /// <summary>
    /// A flag indicating whether the entry is a directory.
    /// </summary>
    IsDirectory = 6u,

    /// <summary>
    /// The uncompressed size of the entry.
    /// </summary>
    Size = 7u,

    /// <summary>
    /// Additional file attributes.
    /// </summary>
    Attributes = 9u,

    /// <summary>
    /// The creation time of the entry, stored as filetime.
    /// </summary>
    CreationTime = 10u,

    /// <summary>
    /// The last access time of the entry, stored as filetime.
    /// </summary>
    LastAccessTime = 11u,

    /// <summary>
    /// The last write time of the entry, stored as filetime.
    /// </summary>
    LastWriteTime = 12u,

    /// <summary>
    /// Indicates whether the entry content is encrypted.
    /// </summary>
    Encrypted = 15u,

    /// <summary>
    /// The CRC of the entry.
    /// </summary>
    Crc = 19u,

    /// <summary>
    /// Indicates a file that could not be compressed (?).
    /// </summary>
    IsAntiFile = 21u,

    /// <summary>
    /// The compression method of the entry.
    /// </summary>
    Method = 22u,

    /// <summary>
    /// An optional comment for the current entry.
    /// </summary>
    Comment = 28u,
}