using System.IO;
using Xunit;

namespace SevenZipTests;

public class ArchiveModificationTests
{
    [Theory]
    [InlineData("test-data/archive.7z")]
    public void RemoveFileAndDirectory(string originalArchivePath)
    {
        var archivePath = "modify-test.7z";

        File.Copy(originalArchivePath, archivePath, overwrite: true);

        using var writer = new SevenZip.ArchiveWriter(archivePath);

        foreach (var entry in writer.ExistingEntries)
        {
            if (entry.Path.Contains("folder"))
            {
                writer.DeleteEntry(entry.Index);
            }
        }

        writer.Compress();

        Assert.Equal(2, writer.ExistingEntries.Count);
    }
}
