#if NET6_0_OR_GREATER
using SevenZip;
using SevenZip.Specialized;
using System.IO;
using Xunit;

namespace SevenZipTests;

public class RepackerTests
{
    [Fact]
    public void RepackArchive()
    {
        using var outputStream = new MemoryStream();
        using var reader = new ArchiveReader("test-data/archive.7z");


        using (var writer = new ArchiveWriter(ArchiveFormat.SevenZip, outputStream, leaveOpen: true))
        {
            const RepackerFlags flags = 
                RepackerFlags.UseFileStreamForOversizedArchiveEntries |
                RepackerFlags.DisposeArchiveEntryStreamAfterUse;

            var repacker = new InMemoryArchiveRepacker(
                reader, writer, flags, 1024, oversizedEntryThreshold: 10
            );

            repacker.Repack(_ => true, entry => entry.Path);
            outputStream.Seek(0, SeekOrigin.Begin);
        }

        using var repackedArchiveReader = new ArchiveReader(outputStream, leaveOpen: true);

        Assert.Equal(reader.Format, repackedArchiveReader.Format);
        Assert.Equal(reader.UncompressedArchiveSize, repackedArchiveReader.UncompressedArchiveSize);
        Assert.Equal(reader.Entries.Count, repackedArchiveReader.Entries.Count);
    }
}
#endif