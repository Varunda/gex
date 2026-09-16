using System;
using System.IO;
using SevenZip.Interop;

namespace SevenZip.Detail;

/// <summary>
/// The <see cref="ArchiveEntryList"/> opens an existing archive and obtains all
/// existing entries.
/// </summary>
internal sealed class ArchiveEntryList
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveEntryList"/> class which
    /// opens an archive and reads its file entries. Afterwards, the archive is closed
    /// again immediately.
    /// </summary>
    /// <param name="reader">The archive reader instance.</param>
    /// <param name="stream">The stream representing the archive.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the archive cannot be opened.
    /// </exception>
    public ArchiveEntryList(in IArchiveReader reader, in Stream stream)
    {
        using var guard = new OpenArchiveGuard(reader, stream);

        guard.EnsureOpened();

        var count = reader.GetNumberOfItems();
        var array = new ArchiveEntry[count];
        var uncompressedArchiveSize = 0ul;

        for (var index = 0u; index < count; ++index)
        {
            var entry = new ArchiveEntry()
            {
                Attributes = (FileAttributes)GetProperty(reader, index, ArchiveEntryProperty.Attributes, union => union.AsUInt32()),
                Comment = GetProperty(reader, index, ArchiveEntryProperty.Comment, union => union.AsString()),
                Crc = GetProperty(reader, index, ArchiveEntryProperty.Crc, union => union.AsUInt32()),
                CreationTime = GetProperty(reader, index, ArchiveEntryProperty.CreationTime, union => union.AsDateTime()),
                Encrypted = GetProperty(reader, index, ArchiveEntryProperty.Encrypted, union => union.AsBool()),
                Path = GetProperty(reader, index, ArchiveEntryProperty.Path, union => union.AsString()),
                Index = (int)index,
                IsDirectory = GetProperty(reader, index, ArchiveEntryProperty.IsDirectory, union => union.AsBool()),
                LastAccessTime = GetProperty(reader, index, ArchiveEntryProperty.LastAccessTime, union => union.AsDateTime()),
                LastWriteTime = GetProperty(reader, index, ArchiveEntryProperty.LastWriteTime, union => union.AsDateTime()),
                Method = GetProperty(reader, index, ArchiveEntryProperty.Method, union => union.AsString()),
                UncompressedSize = GetProperty(reader, index, ArchiveEntryProperty.Size, union => union.AsUInt64())
            };

            array[index] = entry;
            uncompressedArchiveSize += entry.UncompressedSize;
        }

        Entries = array;
        UncompressedArchiveSize = uncompressedArchiveSize;
    }

    /// <summary>
    /// Gets an array containing all entries within the opened archive.
    /// </summary>
    public ArchiveEntry[] Entries { get; }

    /// <summary>
    /// Gets the total uncompressed size, in bytes, of all entries in the archive.
    /// </summary>
    public ulong UncompressedArchiveSize { get; }

    /// <summary>
    /// Reads all entries from the specified archive stream using the provided archive reader.
    /// </summary>
    /// <param name="reader">
    /// The archive reader used to interpret the format and contents of the archive.
    /// </param>
    /// <param name="stream">
    /// The input stream containing the archive data to read. The stream must be readable and 
    /// positioned at the start of the archive.
    /// </param>
    /// <param name="uncompressedArchiveSize">
    /// When this method returns, contains the total uncompressed size of the archive, in bytes.
    /// </param>
    /// <returns>
    /// An array of ArchiveEntry objects representing all entries found in the archive. The 
    /// array is empty if the archive contains no entries.
    /// </returns>
    public static ArchiveEntry[] Read(in IArchiveReader reader, in Stream stream, out ulong uncompressedArchiveSize)
    {
        var entryList = new ArchiveEntryList(reader, stream);

        uncompressedArchiveSize = entryList.UncompressedArchiveSize;
        return entryList.Entries;
    }

    private static T GetProperty<T>(IArchiveReader reader, uint index, ArchiveEntryProperty property, Func<Union, T> convert)
    {
        var union = default(Union);

        reader.GetProperty(index, property, ref union);
        return convert(union);
    }
}
