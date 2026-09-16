using System;

namespace SevenZip.Detail;

/// <summary>
/// <see cref="TargetPath"/> provides methods to create the output path for an <see cref="ArchiveEntry"/>,
/// but additionally ensures that the resulting path is valid.
/// </summary>
internal static class TargetPath
{
    /// <summary>
    /// Concatenates the <paramref name="baseDirectory"/> and the given <paramref name="path"/>
    /// to an output path.
    /// 
    /// If <paramref name="path"/> contains relative directory markers (..), only the last part
    /// of the path is being used, e.g. <c>root/../../somewhere/file.txt</c> results in
    /// <c>somewhere/file.txt</c>.
    /// 
    /// If <paramref name="path"/> contains a rooted path, like <c>C:\file.txt</c>, 
    /// or points to a directory outside <paramref name="baseDirectory"/>, an exception
    /// is thrown since this is a potential security issue.
    /// </summary>
    /// <param name="baseDirectory">
    /// The base directory to extract an archive entry to.
    /// </param>
    /// <param name="path">
    /// The path to append to the <paramref name="baseDirectory"/>
    /// </param>
    /// <returns>The resulting output path.</returns>
    /// <exception cref="System.IO.IOException">
    /// Thrown if the resulting output path would be outside the given <paramref name="baseDirectory"/>.
    /// </exception>
    public static string GetSecureTargetPathOrThrow(string baseDirectory, string path)
    {
        return GetSecureTargetPathOrThrow(baseDirectory, path, isCanonicalBaseDirectory: false);
    }

    /// <summary>
    /// Concatenates the <paramref name="baseDirectory"/> and the given <paramref name="path"/>
    /// to an output path.
    /// 
    /// If <paramref name="path"/> contains relative directory markers (..), only the last part
    /// of the path is being used, e.g. <c>root/../../somewhere/file.txt</c> results in
    /// <c>somewhere/file.txt</c>.
    /// 
    /// If <paramref name="path"/> contains a rooted path, like <c>C:\file.txt</c>, 
    /// or points to a directory outside <paramref name="baseDirectory"/>, an exception
    /// is thrown since this is a potential security issue.
    /// </summary>
    /// <param name="baseDirectory">
    /// The base directory to extract an archive entry to.
    /// </param>
    /// <param name="path">
    /// The path to append to the <paramref name="baseDirectory"/>
    /// </param>
    /// <param name="isCanonicalBaseDirectory">
    /// If set to <c>true</c>, <paramref name="baseDirectory"/> will be used as passed. Otherwise,
    /// its full path will be obtained by calling <see cref="System.IO.Path.GetFullPath(string)"/>.
    /// </param>
    /// <returns>The resulting output path.</returns>
    /// <exception cref="System.IO.IOException">
    /// Thrown if the resulting output path would be outside the given <paramref name="baseDirectory"/>.
    /// </exception>
    public static string GetSecureTargetPathOrThrow(string baseDirectory, string path, bool isCanonicalBaseDirectory)
    {
        var canonicalBaseDir = isCanonicalBaseDirectory ? baseDirectory : System.IO.Path.GetFullPath(baseDirectory);
        var canonicalTargetPath = System.IO.Path.Combine(canonicalBaseDir, Sanitize(path));

        if (!canonicalTargetPath.StartsWith(canonicalBaseDir, StringComparison.OrdinalIgnoreCase))
        {
            throw new System.IO.IOException(
                $"The archive entry path {path} points to a directory outside of {canonicalBaseDir}, which is a security violation (CVE-2024-48510).");
        }

        return canonicalTargetPath;
    }

    /// <summary>
    /// Removes any relative directory markers in the given path.
    /// </summary>
    /// <param name="path">
    /// The path to evaluate.
    /// </param>
    /// <returns>
    /// <paramref name="entry"/>, or, if it contains any relative directory
    /// markers, the last part of the path only. <c>../dir/file.txt</c> would result in
    /// <c>dir/file.txt</c>, for example.
    /// </returns>
    internal static string Sanitize(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return default;
        }

        var index = path.LastIndexOf("..");

        if (index < 0)
        {
            return path;
        }

        for (; index < path.Length && IsRelativePathPart(path[index]); ++index)
        {
            ;
        }

        return path.Substring(index);

        static bool IsRelativePathPart(char c) => c is '\\' or '/' or '.';
    }
}
