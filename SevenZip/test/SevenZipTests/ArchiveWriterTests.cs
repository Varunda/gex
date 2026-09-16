using System.IO;
using SevenZip;
using Xunit;

namespace SevenZipTests;

public class ArchiveWriterTests
{
    const string SampleTextFilePath = "test-file.txt";

    const string SampleBinaryFilePath = "test-file.bin";

    const string SampleTextFileContent = "This is the content of a test file.";

    static readonly byte[] SampleBinaryFileContent = [0x00, 0x01, 0x02, 0x04, 0x05, 0x06];

    [Theory]
    [InlineData(ArchiveFormat.SevenZip)]
    public void CreateArchive(ArchiveFormat format)
    {
        File.WriteAllText(SampleTextFilePath, SampleTextFileContent);

        try
        {
            using (var writer = new ArchiveWriter(format, "writer-test.archive"))
            {
                writer.AddFile("dir/file1.txt", SampleTextFilePath);
                writer.AddFile("dir/file2.bin", new MemoryStream(SampleBinaryFileContent), leaveOpen: false);
                writer.Compress();
            }

            using (var reader = new ArchiveReader("writer-test.archive"))
            {
                var entries = reader.Entries;

                Assert.Equal(2, entries.Count);

                foreach (var entry in entries)
                {
                    Assert.True(entry.UncompressedSize > 0UL);
                    Assert.True(entry.Path is "dir\\file1.txt" or "dir\\file2.bin");
                }
            }
        }
        finally
        {
            File.Delete(SampleTextFilePath);
            File.Delete("writer-test.archive");
        }
    }

    [Theory]
    [InlineData(ArchiveFormat.Zip, EncryptionMethod.Aes256, "test", "AES-256 Store")]
    [InlineData(ArchiveFormat.Zip, EncryptionMethod.Aes192, "test", "AES-192 Store")]
    [InlineData(ArchiveFormat.Zip, EncryptionMethod.Aes128, "test", "AES-128 Store")]
    public void CreateEncryptedArchive(ArchiveFormat format, EncryptionMethod encryptionMethod, string key, string expectedMethodString)
    {
        File.WriteAllText(SampleTextFilePath, SampleTextFileContent);

        var archivePath = $"writer-test-{encryptionMethod}.{format}";
        var p = System.IO.Path.GetFullPath(archivePath);
        try
        {
            using (var writer = new ArchiveWriter(format, archivePath, config: new ArchiveConfig(password: key)))
            {
                writer.AddFile("dir/file1.txt", SampleTextFilePath);
                writer.AddFile("dir/file2.bin", new MemoryStream(SampleBinaryFileContent), leaveOpen: false);
                writer.Compress(new CompressProperties()
                {
                    CompressionLevel = CompressionLevel.Ultra,
                    EncryptionMethod = encryptionMethod,
                    CompressionMethod = CompressionMethod.Lzma,
                });
            }

            using (var reader = new ArchiveReader(archivePath, config: new ArchiveConfig(password: key)))
            {
                var entries = reader.Entries;

                Assert.Equal(2, entries.Count);

                foreach (var entry in entries)
                {
                    Assert.True(entry.UncompressedSize > 0UL);
                    Assert.True(entry.Path is "dir\\file1.txt" or "dir\\file2.bin");

                    Assert.Contains(expectedMethodString, entry.Method);
                }
            }
        }
        finally
        {
            File.Delete(SampleTextFilePath);
            File.Delete(archivePath);
        }
    }

    [Fact]
    public void CreateArchiveWithEmptyDirectory()
    {
        using var stream = new MemoryStream();
        using var writer = new ArchiveWriter(ArchiveFormat.SevenZip, stream, leaveOpen: true);

        writer.AddDirectory("empty-dir");
        writer.Compress();
        stream.Seek(0, SeekOrigin.Begin);

        using var reader = new ArchiveReader(stream, leaveOpen: true);

        var entries = reader.Entries;
        var entry = entries[0];

        Assert.Single(entries);
        Assert.True(entry.IsDirectory);
        Assert.Equal("empty-dir", entry.Path);
    }
}