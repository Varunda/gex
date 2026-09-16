using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SevenZip.Detail;

/// <summary>
/// The <see cref="ArchiveUpdateContext"/> manages any changes of an archive including
/// new, modified and deleted files.
/// </summary>
internal sealed class ArchiveUpdateContext
{
    private readonly Dictionary<int, ArchiveUpdateEntry> _updates = [];
    private readonly List<ArchiveUpdateEntry> _inserts = [];
    private readonly List<ArchiveEntry> _existingArchiveEntries = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveUpdateContext"/> class.
    /// </summary>
    /// <param name="entryList">
    /// The <see cref="ArchiveEntryList"/> containing the entries that already exist in the target archive. This is
    /// required to manage the entry indices and track if a file is new or modified.
    /// </param>
    public ArchiveUpdateContext(in ArchiveEntryList entryList) => Reset(entryList);

    /// <summary>
    /// Gets the total number of entries that will be in the archive after the next compress operation.
    /// </summary>
    public int NewArchiveEntryCount => _existingArchiveEntries.Count + _inserts.Count - GetDeleteOperationCount();

    /// <summary>
    /// Gets a value indicating whether the original archive was empty.
    /// </summary>
    public bool IsEmptyArchive => _existingArchiveEntries.Count == 0;

    /// <summary>
    /// Gets the size the entire archive content requires when being extracted,
    /// in bytes.
    /// </summary>
    public ulong UncompressedArchiveSize { get; private set; }

    /// <summary>
    /// Gets a collection containing the already existing entries.
    /// </summary>
    public IReadOnlyList<ArchiveEntry> ArchiveEntries => _existingArchiveEntries;

    /// <summary>
    /// Creates an <see cref="ArchiveIndexBuffer"/> which maps the new indices to the indices
    /// of the existing archive entries and provides access to the entries which need to be
    /// modified or inserted.
    /// 
    /// The created instance contains the indices for the current state of the context. Changes
    /// to the <see cref="ArchiveUpdateContext"/> which are made afterwards will not be reflected.
    /// Instead, a new instance must be created in that case.
    /// </summary>
    /// <returns>A new instance of the <see cref="ArchiveIndexBuffer"/> containing the new index map.</returns>
    public ArchiveIndexBuffer CreateArchiveIndexBuffer()
    {
        return new ArchiveIndexBuffer(_existingArchiveEntries, _updates, _inserts);
    }

    /// <summary>
    /// Resets the context to an initial state with the given entries.
    /// </summary>
    /// <param name="entryList">
    /// The <see cref="ArchiveEntryList"/> containing the entries that already exist in the associated archive.
    /// </param>
    public void Reset(in ArchiveEntryList entryList)
    {
        _inserts.Clear();
        _updates.Clear();
        _existingArchiveEntries.Clear();
        _existingArchiveEntries.Capacity = entryList?.Entries.Length ?? 0;
        _existingArchiveEntries.AddRange(entryList?.Entries ?? []);

        UncompressedArchiveSize = entryList?.UncompressedArchiveSize ?? 0;
    }

    /// <summary>
    /// Adds a new entry that refers to an existing file.
    /// </summary>
    /// <param name="archivePath">
    /// The name within the archive.
    /// </param>
    /// <param name="path">
    /// The path to the file to add.
    /// </param>
    /// <returns>
    /// The temporary index for the new file.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if any of the arguments is <c>null</c>.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Thrown if <paramref name="path"/> does not refer to an existing file.
    /// </exception>
    public int Add(string archivePath, string path)
    {
        EnsureArgumentNotNull(nameof(archivePath), archivePath);
        EnsureArgumentNotNull(nameof(path), path);
        EnsureFileExists(path);

        return Insert(ArchiveUpdateEntry.Add(archivePath, path));
    }

    /// <summary>
    /// Adds a new entry that refers to a stream.
    /// </summary>
    /// <param name="archivePath">
    /// The name within the archive.
    /// </param>
    /// <param name="stream">
    /// The stream to read the contents from.
    /// </param>
    /// <param name="leaveOpen">
    /// <c>true</c> to leave the <paramref name="stream"/> open after use,
    /// <c>false</c> to dispose it.
    /// </param>
    /// <returns>
    /// The temporary index for the new file.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if any of the arguments is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if the given <paramref name="stream"/> does not support read operations.
    /// </exception>
    public int Add(string archivePath, Stream stream, bool leaveOpen)
    {
        EnsureArgumentNotNull(nameof(archivePath), archivePath);
        EnsureArgumentNotNull(nameof(stream), stream);

        if (stream.CanRead == false)
        {
            throw new ArgumentException("The given stream must support read operations.");
        }

        return Insert(ArchiveUpdateEntry.Add(archivePath, stream, leaveOpen));
    }

    /// <summary>
    /// Marks an existing entry for deletion.
    /// </summary>
    /// <param name="index">
    /// The index of the entry to delete.
    /// </param>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown if the given <paramref name="index"/> does not refer to an existing
    /// entry.
    /// </exception>
    public void Delete(int index)
    {
        EnsureExistingArchiveEntryIndex(index);

        _updates[index] = ArchiveUpdateEntry.Delete();
    }

    /// <summary>
    /// Adds an empty directory to the archive.
    /// </summary>
    /// <param name="archivePath">The path within the archive.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if any of the arguments is <c>null</c>.
    /// </exception>
    public void AddDirectory(string archivePath)
    {
        EnsureArgumentNotNull(nameof(archivePath), archivePath);
        Insert(ArchiveUpdateEntry.AddDirectory(archivePath));
    }

    /// <summary>
    /// Replaces an existing entry with the contents of another file.
    /// </summary>
    /// <param name="index">
    /// The index of the entry to modify.
    /// </param>
    /// <param name="path">
    /// The path to the file to read the new contents from.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if any of the arguments is <c>null</c>.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Thrown if <paramref name="path"/> does not refer to an existing file.
    /// </exception>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown if the given <paramref name="index"/> does not refer to an existing
    /// entry.
    /// </exception>
    public void Replace(int index, string path)
    {
        EnsureExistingArchiveEntryIndex(index);
        EnsureArgumentNotNull(nameof(path), path);
        EnsureFileExists(path);

        var archivePath = _existingArchiveEntries[index].Path;

        _updates[index] = ArchiveUpdateEntry.Update(archivePath, path);
    }

    /// <summary>
    /// Replaces an existing entry with the contents of the given stream.
    /// </summary>
    /// <param name="index">
    /// The index of the entry to modify.
    /// </param>
    /// <param name="stream">
    /// The stream to read the contents from.
    /// </param>
    /// <param name="leaveOpen">
    /// <c>true</c> to leave the <paramref name="stream"/> open after use,
    /// <c>false</c> to dispose it.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if any of the arguments is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if the given <paramref name="stream"/> does not support read operations.
    /// </exception>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown if the given <paramref name="index"/> does not refer to an existing
    /// entry.
    /// </exception>
    public void Replace(int index, Stream stream, bool leaveOpen)
    {
        EnsureExistingArchiveEntryIndex(index);
        EnsureArgumentNotNull(nameof(stream), stream);

        if (stream.CanRead == false)
        {
            throw new ArgumentException("The given stream must support read operations.");
        }

        var archivePath = _existingArchiveEntries[index].Path;

        _updates[index] = ArchiveUpdateEntry.Update(archivePath, stream, leaveOpen);
    }

    private int Insert(ArchiveUpdateEntry entry)
    {
        var index = _existingArchiveEntries.Count + _inserts.Count;

        _inserts.Add(entry);
        return index;
    }

    private int GetDeleteOperationCount()
    {
        return _updates.Values.Count(entry => entry.UpdateOperation == ArchiveUpdateOperation.Delete);
    }

    private void EnsureExistingArchiveEntryIndex(int index)
    {
        if (_existingArchiveEntries.Any(entry => entry.Index == index))
        {
            return;
        }

        throw new IndexOutOfRangeException($"The index {index} does not refer to an existing archive entry.");
    }

    private static void EnsureFileExists(string path)
    {
            if (File.Exists(path))
            {
                return;
            }

            throw new FileNotFoundException($"The file '{path}' does not exist.");
        }

    private static void EnsureArgumentNotNull<T>(string name, T arg) where T : class
    {
        if (arg != null)
        {
            return;
        }

        throw new ArgumentNullException(name);
    }
}