using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SevenZip;
using SevenZip.Detail;
using SevenZip.Interop;
using Xunit;

namespace SevenZipTests;

public sealed class ArchiveEntryListTests
{
    [Theory]
    [InlineData("test-data/archive.7z", 35ul, 7, ArchiveFormat.SevenZip)]
    [InlineData("test-data/archive.zip", 35ul, 6, ArchiveFormat.Zip)]
    public void CreateArchiveEntryListAndValidateUncompressedArchiveSize(
        string path, ulong expectedUncompressedSize, int expectedEntryCount, ArchiveFormat format)
    {
        using var stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);

        var config = new ArchiveConfig();
        var handle = Native.LoadLibrary(config.NativeLibraryPath);
        var reader = ComObjectFactory.CreateObject<IArchiveReader>(handle, format);

        var archiveEntryList = new ArchiveEntryList(reader, stream);

        Assert.Equal(expectedUncompressedSize, archiveEntryList.UncompressedArchiveSize);
        Assert.Equal(expectedEntryCount, archiveEntryList.Entries.Length);
    }
}
