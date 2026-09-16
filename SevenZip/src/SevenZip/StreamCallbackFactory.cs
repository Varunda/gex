using System;
using System.IO;
using SevenZip.Detail;

namespace SevenZip;

/// <summary>
/// The <see cref="StreamCallbackFactory"/> offers methods to create the callbacks used by the
/// <see cref="ArchiveReader"/> when obtaining the <see cref="Stream"/> to extract the content
/// of an <see cref="ArchiveEntry"/>.
/// </summary>
public static class StreamCallbackFactory
{
    /// <summary>
    /// Creates a callback which creates a <see cref="FileStream"/> in the given <paramref name="targetDir"/>
    /// to extract the contents of the current entry.
    /// 
    /// Make sure to pass <see cref="ArchiveFlags.CloseArchiveEntryStreamAfterExtraction"/> to the
    /// <see cref="ArchiveReader"/>'s <c>Extract</c> (or <c>ExtractAll</c>) method. Otherwise, streams
    /// might not be disposed directly.
    /// </summary>
    /// <param name="targetDir">
    /// The target directory to extract the archive contents to.
    /// </param>
    /// <param name="flags">
    /// Additional flags to specify the behavior of the callback. Relevant flags are:
    /// <list type="bullet">
    ///     <item><see cref="ArchiveFlags.FlattenArchiveEntryPaths"/></item>
    ///     <item><see cref="ArchiveFlags.SkipExistingFilesInTargetDir"/></item>
    /// </list>
    /// 
    /// Other flags are not used by this method.
    /// </param>
    /// <returns></returns>
    public static Func<ArchiveEntry, Stream> FileStreams(string targetDir, ArchiveFlags flags)
    {
        return FileStreams(targetDir, isCanonicalTargetDir: false, flags: flags);
    }

    internal static Func<ArchiveEntry, Stream> FileStreams(string targetDir, bool isCanonicalTargetDir, ArchiveFlags flags)
    {
        return (entry) =>
        {
            // In case any of these operations fail they will be caught within the `IArchiveExtractCallback`

            var flattenArchiveEntryPaths = (flags & ArchiveFlags.FlattenArchiveEntryPaths) != 0;
            var archiveEntryPath = flattenArchiveEntryPaths ? Path.GetFileName(entry.Path) : entry.Path;

            var path = TargetPath.GetSecureTargetPathOrThrow(targetDir, archiveEntryPath, isCanonicalTargetDir);
            var directory = entry.IsDirectory ? path : Path.GetDirectoryName(path);

            if (SkipExtractOperation(path, flags))
            {
                return default;
            }

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory) && !flattenArchiveEntryPaths)
            {
                Directory.CreateDirectory(directory);
            }

            return entry.IsDirectory
                ? default
                : File.Open(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        };
    }

    private static bool SkipExtractOperation(string path, ArchiveFlags flags)
    {
        return ((flags & ArchiveFlags.SkipExistingFilesInTargetDir) != 0)
            && File.Exists(path);
    }
}
