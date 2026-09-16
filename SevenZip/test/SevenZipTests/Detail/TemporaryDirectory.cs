using System;
using System.IO;

namespace SevenZipTests.Detail;

internal sealed class TemporaryDirectory : IDisposable
{
    public TemporaryDirectory(string name)
    {
        var baseDirectory = System.IO.Path.GetTempPath();
        var path = System.IO.Path.Combine(baseDirectory, name);

        Directory.CreateDirectory(path);
        Path = path;
    }

    public string Path { get; }

    public void Dispose()
    {
        Directory.Delete(Path, true);
    }
}