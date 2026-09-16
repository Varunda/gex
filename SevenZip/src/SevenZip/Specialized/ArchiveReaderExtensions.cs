using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SevenZip.Specialized;

/// <summary>
/// Extension methods for the <see cref="ArchiveReader"/> which belong
/// to the more specific features.
/// </summary>
public static class ArchiveReaderExtensions
{
    const int MaxCapacity = InMemoryArchiveRepacker.MaxCapacity;

    /// <summary>
    /// Extracts all entries that match the predicate given in the
    /// <paramref name="extractOptions"/> and passes a bulk of
    /// streams that were extracted in-memory to the asynchronous
    /// <paramref name="consumeExtractResults"/> function which can
    /// be used to read the extracted contents.
    /// </summary>
    /// <param name="reader">
    /// The <see cref="ArchiveReader"/> calling this method.
    /// </param>
    /// <param name="consumeExtractResults">
    /// The callback to forward the bulks of extracted files to. The
    /// result of this asynchronous callback is awaited before the
    /// next bulk is being extracted.
    /// </param>
    /// <param name="extractOptions">
    /// Options to configure the extract operation or <c>null</c>, to
    /// use the defaults. See <see cref="BulkExtractOptions"/> for
    /// details.
    /// </param>
    /// <returns>
    /// A task which completes once all files are extracted.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a file within the archive exceeds the maximum size.
    /// </exception>
    public static async Task BulkExtract(
        this ArchiveReader reader,
        Func<IEnumerable<(ArchiveEntry, Stream)>, Task> consumeExtractResults,
        BulkExtractOptions extractOptions = null)
    {
        await BulkExtract(reader, 0, (_, e) => consumeExtractResults(e), extractOptions);
    }

    /// <summary>
    /// Extracts all entries that match the predicate given in the
    /// <paramref name="extractOptions"/> and passes a bulk of
    /// streams that were extracted in-memory to the asynchronous
    /// <paramref name="consumeExtractResults"/> function which can
    /// be used to read the extracted contents.
    /// </summary>
    /// <typeparam name="TState">The type of the state parameter.</typeparam>
    /// <param name="reader">
    /// The <see cref="ArchiveReader"/> calling this method.
    /// </param>
    /// <param name="state">
    /// A user defined state which will be forwarded to the
    /// <paramref name="consumeExtractResults"/> callback.
    /// </param>
    /// <param name="consumeExtractResults">
    /// The callback to forward the bulks of extracted files to. The
    /// result of this asynchronous callback is awaited before the
    /// next bulk is being extracted.
    /// </param>
    /// <param name="extractOptions">
    /// Options to configure the extract operation or <c>null</c>, to
    /// use the defaults. See <see cref="BulkExtractOptions"/> for
    /// details.
    /// </param>
    /// <returns>
    /// A task which completes once all files are extracted.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a file within the archive exceeds the maximum size.
    /// </exception>
    public static async Task BulkExtract<TState>(
        this ArchiveReader reader,
        TState state,
        Func<TState, IEnumerable<(ArchiveEntry, Stream)>, Task> consumeExtractResults,
        BulkExtractOptions extractOptions = null)
    {
        var options = extractOptions ?? new BulkExtractOptions { };
        var oversizedEntryThreshold = (ulong)(options.OversizedEntryThreshold ?? MaxCapacity);
        var entries = new Dictionary<int, (ArchiveEntry, Stream)>();

        using var pool = ScopedStreamPoolFactory.Create(
            reader,
            options.UseNativeStreamPool, 
            options.StreamPoolCapacity);

        using var transaction = new ExtractTransaction(reader, ArchiveFlags.None);

        foreach (var entry in reader.Entries)
        {
            if (options.Predicate is not null && !options.Predicate(entry))
            {
                continue;
            }

            if (entry.IsDirectory)
            {
                entries.Add(entry.Index, (entry, Stream.Null));
                continue;
            }

            if (entry.UncompressedSize > oversizedEntryThreshold)
            {
#if NET6_0_OR_GREATER
                if (options.UseFileStreamForOversizedEntries)
                { 
                    var preallocationSize = (long)entry.UncompressedSize;
                    var stream = SevenZip.Detail.TempFile.Create(
                        reader.Config, deleteOnClose: true, preallocationSize);

                    entries.Add(entry.Index, (entry, stream));
                    continue;
                }
#endif
                throw new InvalidOperationException($"The entry with '{entry.Path}' exceeds the allowerd stream size of {oversizedEntryThreshold} bytes.");
            }
            
            var size = (int)entry.UncompressedSize;
            if (size > pool.Capacity)
            {
                entries.Add(entry.Index, (entry, new MemoryStream(size)));
                continue;
            }

            if (pool.CanCreateStreamWithCapacity(size) == false)
            {
                await ProcessPendingEntries(state, consumeExtractResults, entries, transaction, pool, options);
            }

            entries.Add(entry.Index, (entry, pool.CreateStream(size)));
        }

        await ProcessPendingEntries(state, consumeExtractResults, entries, transaction, pool, options);
    }

    private static async Task ProcessPendingEntries<TState>(
        TState state,
        Func<TState, IEnumerable<(ArchiveEntry, Stream)>, Task> consumeExtractResults,
        Dictionary<int, (ArchiveEntry, Stream)> entries,
        ExtractTransaction transaction,
        IScopedStreamPool pool,
        BulkExtractOptions options)
    {
        if (entries.Count == 0)
        {
            return;
        }

        transaction.Extract(entries.Keys, index => entries[index].Item2);

        foreach (var (_, stream) in entries.Values)
        {
            stream.Seek(0, SeekOrigin.Begin);
        }

        await consumeExtractResults(state, entries.Values);

        if (options.DisposeArchiveEntryStreamAfterUse)
        {
            foreach (var (_, stream) in entries.Values)
            {
                stream.Dispose();
            }
        }

        entries.Clear();
        pool.Discard();
    }
}
