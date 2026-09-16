using System;
using System.IO;
using SevenZip;
using Xunit;

namespace SevenZipTests;

public class PathEnumeratorTests
{
    [Fact]
    public void EnumeratePathsWithAbsoluteStrategy()
    {
        EnumeratePaths(
            NamingStrategy.Absolute,
            path => Path.GetPathRoot(path)!.Length);
    }

    [Fact]
    public void EnumeratePathsWithRelativeToTopDirInclusiveStrategy()
    {
        EnumeratePaths(
            NamingStrategy.RelativeToTopDirectoryInclusive, 
            path => 1 + Directory.GetParent(path)!.FullName.Length);
    }

    [Fact]
    public void EnumeratePathsWithRelativeToTopDirExclusiveStrategy()
    {
        EnumeratePaths(
            NamingStrategy.RelativeToTopDirectoryExclusive,
            path => 1 + path.Length);
    }

    [Fact]
    public void EnumeratePathsWithFilenameStrategy()
    {
        EnumeratePaths(
            NamingStrategy.FilenamesOnly,
            _ => 0,
            (path, _) => Path.GetFileName(path));
    }

    private static void EnumeratePaths(NamingStrategy strategy, Func<string, int> substringOffset, Func<string, int, string> transform = null)
    {
        using var root = new Detail.TemporaryDirectory(strategy.ToString());

        transform ??= (path, index) => path.Substring(index);

        var count = 0;
        var offset = substringOffset(root.Path);
        var fileCount = CreateTestFilesAndFolders(root.Path);
        var enumerator = new PathEnumerator(
            root.Path,
            "*",
            recursive: true,
            strategy);

        foreach (var (path, archivePath) in enumerator.EnumerateLazy((_, ex) => throw ex))
        {
            var expected = transform(path, offset);
            count++;

            Assert.Equal(expected, archivePath);
        }

        Assert.Equal(fileCount, count);
    }

    private static int CreateTestFilesAndFolders(string directory)
    {
        var subdir = Directory.CreateDirectory(Path.Combine(directory, "subdir"));

        File.WriteAllText(Path.Combine(directory, "file1.txt"), "test");
        File.WriteAllText(Path.Combine(directory, "file2.txt"), "test");
        File.WriteAllText(Path.Combine(subdir.FullName, "file3.txt"), "test");
        return 3;
    }
}