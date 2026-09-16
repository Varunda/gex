#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SevenZip;
using SevenZip.Specialized;
using Xunit;

namespace SevenZipTests;

public sealed class BulkExtractTests
{
    [Fact]
    public async Task BulkExtractWithFileStreamForOversizedEntries()
    {
        using var archiveReader = new ArchiveReader("test-data/archive.7z");

        // We set OversizedEntryThreshold to 10 to fallback
        // to FileStream for entries larger than 10 bytes.
        var options = new BulkExtractOptions()
        {
            UseFileStreamForOversizedEntries = true,
            OversizedEntryThreshold = 10,
            StreamPoolCapacity = 0,
            DisposeArchiveEntryStreamAfterUse = true,
            UseNativeStreamPool = true,
            Predicate = entry => !entry.IsDirectory,
        };

        var fileStreamCount = 0;
        var buffer = new byte[32];
        var entries = new Dictionary<string, int>();

        await archiveReader.BulkExtract(
            elements =>
            {
                foreach (var (entry, stream) in elements)
                {
                    entries.Add(entry.Path, (int)stream.Length);

                    if (entry.UncompressedSize > (ulong)options.OversizedEntryThreshold)
                    {
                        fileStreamCount++;

                        Assert.IsType<FileStream>(stream);
                    }
                    else
                    {
                        Assert.IsNotType<FileStream>(stream);
                    }

                    var bytesRead = stream.Read(buffer, 0, Math.Min(buffer.Length, (int)stream.Length));

                    Assert.True(bytesRead > 0);
                }

                return Task.CompletedTask;
            }, options);

        Assert.Equal(1, fileStreamCount);
        Assert.Equal(5, entries.Count);
        Assert.Contains(@"archive\folder\file-1.txt", entries as IReadOnlyDictionary<string, int>);
        Assert.Contains(@"archive\folder\file-2.txt", entries as IReadOnlyDictionary<string, int>);
        Assert.Contains(@"archive\folder\file-3.txt", entries as IReadOnlyDictionary<string, int>);
        Assert.Contains(@"archive\folder\file-4.txt", entries as IReadOnlyDictionary<string, int>);
        Assert.Contains(@"archive\file-1.txt", entries as IReadOnlyDictionary<string, int>);
        Assert.Equal(11, entries[@"archive\file-1.txt"]);
        Assert.Equal(6, entries[@"archive\folder\file-1.txt"]);
        Assert.Equal(6, entries[@"archive\folder\file-2.txt"]);
        Assert.Equal(6, entries[@"archive\folder\file-3.txt"]);
        Assert.Equal(6, entries[@"archive\folder\file-4.txt"]);
    }
}
#endif // NET6_0_OR_GREATER