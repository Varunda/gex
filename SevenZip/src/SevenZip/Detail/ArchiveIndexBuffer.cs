using System.Collections.Generic;

namespace SevenZip.Detail;

/// <summary>
/// The <see cref="ArchiveIndexBuffer"/> class manages the mapping of the archive entry indices.
/// 
/// It provides the new index for an existing entry and a reference to the <see cref="ArchiveUpdateEntry"/>
/// for modified or new entries.
/// 
/// An instance represents a snapshot of the <see cref="ArchiveUpdateContext"/> at the time of creation. This
/// class is required by the <see cref="Interop.IArchiveUpdateCallback"/> interface, which is used by the
/// 7z library implementation to obtain the entries to process.
/// </summary>
internal sealed class ArchiveIndexBuffer
{
    private readonly Dictionary<uint, uint> _indexToExistingArchiveIndex = [];
    private readonly IReadOnlyDictionary<int, ArchiveUpdateEntry> _updates;
    private readonly IReadOnlyList<ArchiveUpdateEntry> _inserts;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveIndexBuffer"/> class.
    /// </summary>
    /// <param name="existingArchiveEntries">A list of the already existing entries.</param>
    /// <param name="updates">A dictionary containing the entries to modify or delete.</param>
    /// <param name="inserts">A collection of entries to add to the archive.</param>
    public ArchiveIndexBuffer(
        IReadOnlyList<ArchiveEntry> existingArchiveEntries,
        IReadOnlyDictionary<int, ArchiveUpdateEntry> updates,
        IReadOnlyList<ArchiveUpdateEntry> inserts)
    {
        _updates = updates;
        _inserts = inserts;

        var index = 0u;

        foreach (var entry in existingArchiveEntries)
        {
            // Skip entries that are marked for deletion.

            if (updates.TryGetValue(entry.Index, out var update) && update.UpdateOperation == ArchiveUpdateOperation.Delete)
            {
                continue;
            }

            _indexToExistingArchiveIndex[index++] = (uint)entry.Index;
        }
    }

    /// <summary>
    /// For the current entry identified by <paramref name="index"/>, gets the index that entry
    /// used to have in the already existing archive. Required by 7z to correctly remap entries
    /// which are not modified.
    /// </summary>
    /// <param name="index">The current entry index.</param>
    /// <param name="default">The value to return if the index is not mapped to an existing entry.</param>
    /// <returns>The index the entry has in the existing archive.</returns>
    public uint GetIndexInExistingArchive(uint index, uint @default = uint.MaxValue)
    {
        return _indexToExistingArchiveIndex.TryGetValue(index, out var existingIndex)
            ? existingIndex
            : @default;
    }

    /// <summary>
    /// For the current entry identified by <paramref name="index"/>, tries to get the
    /// corresponding <see cref="ArchiveUpdateEntry"/>, which contains the data for an
    /// entry to replace or add.
    /// </summary>
    /// <param name="index">
    /// The current entry index.
    /// </param>
    /// <param name="entry">
    /// The corresponding entry or <c>null</c>, if the <paramref name="index"/> is not mapped to.
    /// </param>
    /// <returns>
    /// <see langword="true"/>, if the entry could be found; otherwise, <see langword="false"/>.
    /// </returns>
    public bool TryGetArchiveUpdateEntry(uint index, out ArchiveUpdateEntry entry)
    {
        var existingArchiveEntryCount = (uint)_indexToExistingArchiveIndex.Count;

        if (index < existingArchiveEntryCount)
        {
            if (_indexToExistingArchiveIndex.TryGetValue(index, out var existingIndex) &&
                _updates.TryGetValue((int)existingIndex, out entry))
            {
                return true;
            };

            entry = null;
        }
        else
        {
            var newEntryIndex = index - existingArchiveEntryCount;
            if (newEntryIndex < _inserts.Count)
            {
                entry = _inserts[(int)newEntryIndex];
            }
            else
            {
                entry = null;
            }
        }

        return entry != null;
    }
}
