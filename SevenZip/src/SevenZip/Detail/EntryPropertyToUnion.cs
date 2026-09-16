using System.IO;
using SevenZip.Interop;

namespace SevenZip.Detail;

/// <summary>
/// This class provides methods to transforms properties of an <see cref="ArchiveEntry"/> or
/// <see cref="ArchiveUpdateEntry"/> into a <see cref="Union"/>.
/// </summary>
static class EntryPropertyToUnion
{
    /// <summary>
    /// Transforms the given <paramref name="property"/> into a <see cref="Union"/> and assigns
    /// it to the <paramref name="result"/> reference.
    /// 
    /// For string properties, <see cref="Union.Free"/> must be called once the value is no
    /// longer used to avoid memory leaks.
    /// </summary>
    /// <param name="entry">The entry to read the property from.</param>
    /// <param name="property">The property to transform.</param>
    /// <param name="result">The reference to assign the transformed value to.</param>
    /// <returns>
    /// Zero (0) on success.
    /// </returns>
    public static int Convert(ArchiveUpdateEntry entry, ArchiveEntryProperty property, ref Union result)
    {
        result = property switch
        {
            ArchiveEntryProperty.Path => Union.Create(entry.ArchivePath),
            ArchiveEntryProperty.IsDirectory => Union.Create(entry.IsDirectory),
            ArchiveEntryProperty.Size => Union.Create((ulong) entry.Size),
            ArchiveEntryProperty.Attributes => Union.Create((uint)entry.Attributes),
            ArchiveEntryProperty.CreationTime => Union.Create(entry.CreationTime),
            ArchiveEntryProperty.LastAccessTime => Union.Create(entry.LastAccessTime),
            ArchiveEntryProperty.LastWriteTime => Union.Create(entry.LastWriteTime),
            ArchiveEntryProperty.Extension => Union.Create(entry.GetExtension()),
            ArchiveEntryProperty.IsAntiFile => Union.Create(false),
            _ => result
        };

        return 0;
    }

    /// <summary>
    /// Transforms the given <paramref name="property"/> into a <see cref="Union"/> and assigns
    /// it to the <paramref name="result"/> reference.
    /// 
    /// For string properties, <see cref="Union.Free"/> must be called once the value is no
    /// longer used to avoid memory leaks.
    /// </summary>
    /// <param name="entry">The entry to read the property from.</param>
    /// <param name="property">The property to transform.</param>
    /// <param name="result">The reference to assign the transformed value to.</param>
    /// <returns>
    /// Zero (0) on success.
    /// </returns>
    public static int Convert(in ArchiveEntry entry, ArchiveEntryProperty property, ref Union result)
    {
        result = property switch
        {
            ArchiveEntryProperty.Path => Union.Create(entry.Path),
            ArchiveEntryProperty.IsDirectory => Union.Create(entry.IsDirectory),
            ArchiveEntryProperty.Size => Union.Create(entry.UncompressedSize),
            ArchiveEntryProperty.Attributes => Union.Create((uint) entry.Attributes),
            ArchiveEntryProperty.CreationTime => Union.Create(entry.CreationTime),
            ArchiveEntryProperty.LastAccessTime => Union.Create(entry.LastAccessTime),
            ArchiveEntryProperty.LastWriteTime => Union.Create(entry.LastWriteTime),
            ArchiveEntryProperty.Extension => Union.Create(Path.GetExtension(entry.Path)),
            ArchiveEntryProperty.IsAntiFile => Union.Create(false),
            _ => result
        };

        return 0;
    }
}