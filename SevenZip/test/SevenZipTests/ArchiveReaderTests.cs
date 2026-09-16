using System.IO;
using System.Linq;
using SevenZip;
using SevenZip.Extensions;
using Xunit;

namespace SevenZipTests;

public class ArchiveReaderTests
{
    [Fact]
    public void ExtractZipArchiveWithInsecurePath()
    {
        Assert.Throws<IOException>(() =>
        {
            using var reader = new ArchiveReader(
                "test-data/absolute-path-traversal.zip",
                new Detail.ZipPathTraversalArchiveReaderDelegate());

            reader.ExtractAll("result-zip-path-traversal");
        });
    }

    [Fact]
    public void Extract7zArchive()
    {
        const string targetDir = "result-7z-7zs-simple";

        try
        {
            var expectedEntryCount = -1L;
            var counter = new Detail.ArchiveEntryExtractCounter();

            using(var reader = new ArchiveReader("test-data/archive.7z", counter))
            {
                expectedEntryCount = reader.Count;
                reader.ExtractAll(targetDir);
            }

            Assert.Equal(expectedEntryCount, counter.Result);
        }
        finally
        {
            DeleteDirectory(targetDir);
        }
    }
        
    [Fact]
    public void ExtractZipArchive()
    {
        const string targetDir = "result-zip-7zs-simple";

        try
        {
            var expectedEntryCount = -1L;
            var counter = new Detail.ArchiveEntryExtractCounter();

            using (var reader = new ArchiveReader("test-data/archive.zip", counter))
            {
                expectedEntryCount = reader.Count;
                reader.ExtractAll(targetDir);
            }
 
            Assert.Equal(expectedEntryCount, counter.Result);
        }
        finally
        {
            DeleteDirectory(targetDir);
        }
    }

    [Theory]
    [InlineData("test-data/archive-encrypted.zip", "test")]
    public void ExtractEncryptedArchive(string path, string key)
    {
        const string targetDir = "result-encrypted-archive";

        try
        {
            using (var reader = new ArchiveReader(path, null , new ArchiveConfig(key, ignoreOperationErrors: false)))
            {
                Assert.True(IsEncrypted(reader));

                reader.ExtractAll(targetDir);
            }

            var dirInfo = new DirectoryInfo(targetDir);
            var didExtractAtLeastOneFile = false;

            foreach (var fileInfo in dirInfo.EnumerateFiles("*.*", SearchOption.AllDirectories))
            {
                didExtractAtLeastOneFile = true;

                Assert.True(fileInfo.Length > 0);
            }

            Assert.True(didExtractAtLeastOneFile);
        }
        finally
        {
            DeleteDirectory(targetDir);
        }
    }

    [Theory]
    [InlineData("test-data/archive.zip", "flattened-zip")]
    public void ExtractArchiveWithFlattenedArchiveEntryPaths(string path, string targetDir)
    {
        try
        {
            using (var reader = new ArchiveReader(path))
            {
                reader.Extract(IndexList.All, targetDir, ArchiveFlags.FlattenArchiveEntryPaths);
            }

            var fileCount = Directory.GetFiles(targetDir).Length;
            var dirCount = Directory.GetDirectories(targetDir).Length;

            Assert.True(dirCount == 0);
            Assert.True(fileCount == 4);
        }
        finally
        {
            Directory.Delete(targetDir, recursive: true);
        }
    }

    [Theory]
    [InlineData("test-data/archive.zip", "flattened-zip")]
    public void ExtractArchiveWithFlattenedArchiveEntryPathsAndSkipExistingFiles(string path, string targetDir)
    {
        try
        {
            using (var reader = new ArchiveReader(path))
            {
                reader.Extract(IndexList.All, targetDir, ArchiveFlags.FlattenArchiveEntryPaths | ArchiveFlags.SkipExistingFilesInTargetDir);
            }

            var fileCount = Directory.GetFiles(targetDir).Length;
            var dirCount = Directory.GetDirectories(targetDir).Length;

            var content = File.ReadAllText(Path.Combine(targetDir, "file-1.txt"));

            Assert.Equal("file-1-root", content);

            Assert.True(dirCount == 0);
            Assert.True(fileCount == 4);
        }
        finally
        {
            Directory.Delete(targetDir, recursive: true);
        }
    }

    [Theory]
    [InlineData("test-data/archive-encrypted.zip", "wrong")]
    public void ExtractEncryptedArchiveWithWrongPassword(string path, string key)
    {
        const string targetDir = "result-encrypted-archive-wrong-pw";

        try
        {
            using (var reader = new ArchiveReader(path, null, new ArchiveConfig(key, ignoreOperationErrors: false)))
            {
                Assert.True(IsEncrypted(reader));
                Assert.Throws<ArchiveOperationException>(() =>
                {
                    reader.ExtractAll(targetDir);
                });
            }
        }
        finally
        {
            DeleteDirectory(targetDir);
        }
    }

    [Theory]
    [InlineData("test-data/archive.zip", null)]
    [InlineData("test-data/archive-encrypted.zip", "test")]
    public void CanExtractEntries(string path, string key)
    {
        using var reader = new ArchiveReader(path, null, new ArchiveConfig(key));

        var result = reader.CanExtractEntries();

        Assert.True(result);
    }

    private static bool IsEncrypted(ArchiveReader reader)
    {
        return reader.Entries.Any(entry => entry.Encrypted);
    }

    private static void DeleteDirectory(string directory)
    {
        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch
        {
            // nothing
        }
    }
}