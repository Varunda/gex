using System;
using System.Collections.Generic;

namespace SevenZip.Extensions;

/// <summary>
/// Extension methods for the <see cref="ArchiveWriter"/> class.
/// </summary>
public static class ArchiveWriterExtensions
{
    /// <summary>
    /// Adds all files of the given <paramref name="directory"/> to the archive and constructs the
    /// archive paths with a custom <paramref name="selector"/>.
    /// </summary>
    /// <param name="self">
    /// The <see cref="ArchiveWriter"/> calling this method.
    /// </param>
    /// <param name="directory">
    /// The root directory containining the files to add.
    /// </param>
    /// <param name="selector">
    /// A custom callback to construct the archive path from the full file path.
    /// </param>
    /// <param name="error">
    /// An optional callback which is invoked when construction of an archive path fails.
    /// </param>
    /// <returns>
    /// A dictionary containing the full paths of the files that were added and their corresponding
    /// archive indices.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any of the arguments is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="directory"/> is an empty string.
    /// </exception>
    /// <exception cref="System.IO.DirectoryNotFoundException">
    /// Thrown if the directory to iterate over does not exist.
    /// </exception>
    /// <exception cref="System.Security.SecurityException">
    /// Thrown if the directory or a file cannot be accessed.
    /// </exception>
    public static IReadOnlyDictionary<string, int> AddDirectory(this ArchiveWriter self, string directory,
        Func<string, string> selector,
        Action<string, Exception> error = null)
    {
        return AddDirectoryCore(self, directory, "*", recursive: false, selector: selector, error: error);
    }

    /// <summary>
    /// Adds all files of the given <paramref name="directory"/> to the archive and constructs the
    /// archive paths with a custom <paramref name="selector"/>.
    /// </summary>
    /// <param name="self">
    /// The <see cref="ArchiveWriter"/> calling this method.
    /// </param>
    /// <param name="directory">
    /// The root directory containining the files to add.
    /// </param>
    /// <param name="searchPattern">
    /// A search pattern to limit the files to add. <c>"*"</c> will for example add all files, <c>"*.txt"</c>
    /// would only add files with the given extension.
    /// </param>
    /// <param name="selector">
    /// A custom callback to construct the archive path from the full file path.
    /// </param>
    /// <param name="error">
    /// An optional callback which is invoked when construction of an archive path fails.
    /// </param>
    /// <returns>
    /// A dictionary containing the full paths of the files that were added and their corresponding
    /// archive indices.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any of the arguments is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="directory"/> or <paramref name="searchPattern"/> is an empty string.
    /// </exception>
    /// <exception cref="System.IO.DirectoryNotFoundException">
    /// Thrown if the directory to iterate over does not exist.
    /// </exception>
    /// <exception cref="System.Security.SecurityException">
    /// Thrown if the directory or a file cannot be accessed.
    /// </exception>
    public static IReadOnlyDictionary<string, int> AddDirectory(this ArchiveWriter self, string directory,
        string searchPattern,
        Func<string, string> selector,
        Action<string, Exception> error = null)
    {
        return AddDirectoryCore(self, directory, searchPattern, recursive: false, selector: selector, error: error);
    }

    /// <summary>
    /// Adds all files of the given <paramref name="directory"/> to the archive 
    /// and constructs the archive paths with a given <see cref="NamingStrategy"/>
    /// </summary>
    /// <param name="self">
    /// The <see cref="ArchiveWriter"/> calling this method.
    /// </param>
    /// <param name="directory">
    /// The root directory containining the files to add.
    /// </param>
    /// <param name="strategy">
    /// The naming strategy to use. See <see cref="NamingStrategy"/> for details.
    /// </param>
    /// <param name="error">
    /// An optional callback which is invoked when construction of an archive path fails.
    /// </param>
    /// <returns>
    /// A dictionary containing the full paths of the files that were added and their corresponding
    /// archive indices.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any of the arguments is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="directory"/> is an empty string.
    /// </exception>
    /// <exception cref="System.IO.DirectoryNotFoundException">
    /// Thrown if the directory to iterate over does not exist.
    /// </exception>
    /// <exception cref="System.Security.SecurityException">
    /// Thrown if the directory or a file cannot be accessed.
    /// </exception>
    public static IReadOnlyDictionary<string, int> AddDirectory(this ArchiveWriter self, string directory,
        NamingStrategy strategy,
        Action<string, Exception> error = null)
    {
        return AddDirectoryCore(self, directory, "*", recursive: false, strategy: strategy, error: error);
    }

    /// <summary>
    /// Adds all files of the given <paramref name="directory"/> to the archive 
    /// and constructs the archive paths with a given <see cref="NamingStrategy"/>
    /// </summary>
    /// <param name="self">
    /// The <see cref="ArchiveWriter"/> calling this method.
    /// </param>
    /// <param name="directory">
    /// The root directory containining the files to add.
    /// </param>
    /// <param name="searchPattern">
    /// A search pattern to limit the files to add. <c>"*"</c> will for example add all files, <c>"*.txt"</c>
    /// would only add files with the given extension.
    /// </param>
    /// <param name="strategy">
    /// The naming strategy to use. See <see cref="NamingStrategy"/> for details.
    /// </param>
    /// <param name="error">
    /// An optional callback which is invoked when construction of an archive path fails.
    /// </param>
    /// <returns>
    /// A dictionary containing the full paths of the files that were added and their corresponding
    /// archive indices.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any of the arguments is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="directory"/> or <paramref name="searchPattern"/> is an empty string.
    /// </exception>
    /// <exception cref="System.IO.DirectoryNotFoundException">
    /// Thrown if the directory to iterate over does not exist.
    /// </exception>
    /// <exception cref="System.Security.SecurityException">
    /// Thrown if the directory or a file cannot be accessed.
    /// </exception>
    public static IReadOnlyDictionary<string, int> AddDirectory(this ArchiveWriter self, string directory,
        string searchPattern,
        NamingStrategy strategy,
        Action<string, Exception> error = null)
    {
        return AddDirectoryCore(self, directory, searchPattern, recursive: false, strategy: strategy, error: error);
    }


    /// <summary>
    /// Adds all files of the given <paramref name="directory"/> to the archive including files from subdirectories
    /// and constructs the archive paths with a custom <paramref name="selector"/>.
    /// </summary>
    /// <param name="self">
    /// The <see cref="ArchiveWriter"/> calling this method.
    /// </param>
    /// <param name="directory">
    /// The root directory containining the files to add.
    /// </param>
    /// <param name="selector">
    /// A custom callback to construct the archive path from the full file path.
    /// </param>
    /// <param name="error">
    /// An optional callback which is invoked when construction of an archive path fails.
    /// </param>
    /// <returns>
    /// A dictionary containing the full paths of the files that were added and their corresponding
    /// archive indices.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any of the arguments is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="directory"/> is an empty string.
    /// </exception>
    /// <exception cref="System.IO.DirectoryNotFoundException">
    /// Thrown if the directory to iterate over does not exist.
    /// </exception>
    /// <exception cref="System.Security.SecurityException">
    /// Thrown if the directory or a file cannot be accessed.
    /// </exception>
    public static IReadOnlyDictionary<string, int> AddDirectoryRecursive(this ArchiveWriter self, string directory,
        Func<string, string> selector,
        Action<string, Exception> error = null)
    {
        return AddDirectoryCore(self, directory, "*", recursive: true, selector: selector, error: error);
    }

    /// <summary>
    /// Adds all files of the given <paramref name="directory"/> to the archive including files from subdirectories
    /// and constructs the archive paths with a custom <paramref name="selector"/>.
    /// </summary>
    /// <param name="self">
    /// The <see cref="ArchiveWriter"/> calling this method.
    /// </param>
    /// <param name="directory">
    /// The root directory containining the files to add.
    /// </param>
    /// <param name="searchPattern">
    /// A search pattern to limit the files to add. <c>"*"</c> will for example add all files, <c>"*.txt"</c>
    /// would only add files with the given extension.
    /// </param>
    /// <param name="selector">
    /// A custom callback to construct the archive path from the full file path.
    /// </param>
    /// <param name="error">
    /// An optional callback which is invoked when construction of an archive path fails.
    /// </param>
    /// <returns>
    /// A dictionary containing the full paths of the files that were added and their corresponding
    /// archive indices.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any of the arguments is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="directory"/> or <paramref name="searchPattern"/> is an empty string.
    /// </exception>
    /// <exception cref="System.IO.DirectoryNotFoundException">
    /// Thrown if the directory to iterate over does not exist.
    /// </exception>
    /// <exception cref="System.Security.SecurityException">
    /// Thrown if the directory or a file cannot be accessed.
    /// </exception>
    public static IReadOnlyDictionary<string, int> AddDirectoryRecursive(this ArchiveWriter self, string directory,
        string searchPattern,
        Func<string, string> selector,
        Action<string, Exception> error = null)
    {
        return AddDirectoryCore(self, directory, searchPattern, recursive: true, selector: selector, error: error);
    }

    /// <summary>
    /// Adds all files of the given <paramref name="directory"/> to the archive including files from subdirectories
    /// and constructs the archive paths with a given <see cref="NamingStrategy"/>.
    /// </summary>
    /// <param name="self">
    /// The <see cref="ArchiveWriter"/> calling this method.
    /// </param>
    /// <param name="directory">
    /// The root directory containining the files to add.
    /// </param>
    /// <param name="strategy">
    /// The naming strategy to use. See <see cref="NamingStrategy"/> for details.
    /// </param>
    /// <param name="error">
    /// An optional callback which is invoked when construction of an archive path fails.
    /// </param>
    /// <returns>
    /// A dictionary containing the full paths of the files that were added and their corresponding
    /// archive indices.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any of the arguments is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="directory"/> is an empty string.
    /// </exception>
    /// <exception cref="System.IO.DirectoryNotFoundException">
    /// Thrown if the directory to iterate over does not exist.
    /// </exception>
    /// <exception cref="System.Security.SecurityException">
    /// Thrown if the directory or a file cannot be accessed.
    /// </exception>
    public static IReadOnlyDictionary<string, int> AddDirectoryRecursive(this ArchiveWriter self, string directory,
        NamingStrategy strategy,
        Action<string, Exception> error = null)
    {
        return AddDirectoryCore(self, directory, "*", recursive: true, strategy: strategy, error: error);
    }

    /// <summary>
    /// Adds all files of the given <paramref name="directory"/> to the archive including files from subdirectories
    /// and constructs the archive paths with a given <see cref="NamingStrategy"/>
    /// </summary>
    /// <param name="self">
    /// The <see cref="ArchiveWriter"/> calling this method.
    /// </param>
    /// <param name="directory">
    /// The root directory containining the files to add.
    /// </param>
    /// <param name="searchPattern">
    /// A search pattern to limit the files to add. <c>"*"</c> will for example add all files, <c>"*.txt"</c>
    /// would only add files with the given extension.
    /// </param>
    /// <param name="strategy">
    /// The naming strategy to use. See <see cref="NamingStrategy"/> for details.
    /// </param>
    /// <param name="error">
    /// An optional callback which is invoked when construction of an archive path fails.
    /// </param>
    /// <returns>
    /// A dictionary containing the full paths of the files that were added and their corresponding
    /// archive indices.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any of the arguments is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="directory"/> or <paramref name="searchPattern"/> is an empty string.
    /// </exception>
    /// <exception cref="System.IO.DirectoryNotFoundException">
    /// Thrown if the directory to iterate over does not exist.
    /// </exception>
    /// <exception cref="System.Security.SecurityException">
    /// Thrown if the directory or a file cannot be accessed.
    /// </exception>
    public static IReadOnlyDictionary<string, int> AddDirectoryRecursive(this ArchiveWriter self, string directory,
        string searchPattern,
        NamingStrategy strategy,
        Action<string, Exception> error = null)
    {
        return AddDirectoryCore(self, directory, searchPattern, recursive: true, strategy: strategy, error: error);
    }

    private static IReadOnlyDictionary<string, int> AddDirectoryCore(ArchiveWriter writer, string directory,
        string searchPattern,
        bool recursive,
        Func<string, string> selector,
        Action<string, Exception> error = null)
    {
        return AddDirectoryCore(writer, new PathEnumerator(directory, searchPattern, recursive, selector), error);
    }

    private static IReadOnlyDictionary<string, int> AddDirectoryCore(ArchiveWriter writer, string directory,
        string searchPattern,
        bool recursive,
        NamingStrategy strategy,
        Action<string, Exception> error = null)
    {
        return AddDirectoryCore(writer, new PathEnumerator(directory, searchPattern, recursive, strategy), error);
    }

    private static IReadOnlyDictionary<string, int> AddDirectoryCore(ArchiveWriter writer, PathEnumerator enumerator,
        Action<string, Exception> error = null)
    {
        var result = new Dictionary<string, int>();

        foreach (var (path, archivePath) in enumerator.EnumerateLazy(error))
        {
            var index = writer.AddFile(archivePath, path);

            result[path] = index;
        }

        return result;
    }
}
