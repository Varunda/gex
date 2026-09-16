using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace SevenZip;

/// <summary>
/// The <see cref="NamingStrategySelector"/> provides methods to create the selector
/// callback which transforms a file path to an archive path.
/// </summary>
internal static class NamingStrategySelector
{
    /// <summary>
    /// Gets a callback that transforms a file path to the archive path.
    /// </summary>
    /// <param name="strategy">The strategy to use.</param>
    /// <param name="directory">The base directory to use for operations that refer to a common root.</param>
    /// <returns>The selector for the corresponding <paramref name="strategy"/>.</returns>
    public static Func<string, string> Get(NamingStrategy strategy, string directory)
    {
        return strategy switch
        {
            NamingStrategy.RelativeToTopDirectoryInclusive => GetRelativeToTopDirSelector(directory, includeTopDirectoryName: true),
            NamingStrategy.RelativeToTopDirectoryExclusive => GetRelativeToTopDirSelector(directory, includeTopDirectoryName: false),
            NamingStrategy.FilenamesOnly => GetFilenameSelector(),
            _ => GetAbsoluteSelector(),
        };
    }

    private static Func<string, string> GetFilenameSelector()
    {
        return Path.GetFileName;
    }

    private static Func<string, string> GetAbsoluteSelector()
    {
        return path =>
        {
            return Path.GetPathRoot(path) switch
            {
                string root when !string.IsNullOrWhiteSpace(root) => path.Substring(root.Length),
                _ => path,
            };
        };
    }

    [SuppressMessage("ReSharper", "UseIndexFromEndExpression")]
    private static Func<string, string> GetRelativeToTopDirSelector(string directory, bool includeTopDirectoryName)
    {
        var root = Path.GetFullPath(directory);

        if (string.IsNullOrEmpty(root))
        {
            return path => path;
        }

        if (includeTopDirectoryName)
        {
            var parent = Directory.GetParent(root);
            if (parent != null)
            {
                root = parent.FullName;
            }
        }

        root = root.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

        if (root.Length > 0 && root[root.Length - 1] != Path.DirectorySeparatorChar)
        {
            root += Path.DirectorySeparatorChar;
        }

        var offset = root.Length;

        return path => path.StartsWith(root, StringComparison.InvariantCultureIgnoreCase)
            ? path.Substring(offset)
            : path;
    }
}