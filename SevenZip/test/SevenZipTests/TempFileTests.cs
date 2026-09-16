#if NET6_0_OR_GREATER

using System.IO;
using System.Text;
using SevenZip;
using SevenZip.Detail;
using Xunit;

namespace SevenZipTests;

public class TempFileTests
{
    [Fact]
    public void CreateTempFileAndEnsureDeletionAfterClose()
    {
        var config = new ArchiveConfig();
        var content = "Temporary file content";
        var contentBytes = Encoding.UTF8.GetBytes(content); 
        var filename = string.Empty;

        using (var stream = TempFile.Create(config, deleteOnClose: true, preallocationSize: contentBytes.Length))
        {
            filename = stream.Name;

            stream.Write(contentBytes, 0, contentBytes.Length);
            stream.Flush();
            stream.Seek(0, SeekOrigin.Begin);

            using var reader = new StreamReader(stream, encoding: Encoding.UTF8, leaveOpen: true);

            var fileContent = reader.ReadToEnd();

            Assert.True(File.Exists(filename));
            Assert.Equal(content, fileContent);
        }

        Assert.False(File.Exists(filename));
    }
}

#endif // NET6_0_OR_GREATER
