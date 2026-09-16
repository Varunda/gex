using System;
using System.Collections.Generic;
using System.IO;

namespace SevenZip;

/// <summary>
/// The <see cref="PathEnumerator"/> class iterates over all files of a given directory and constructs the corresponding
/// archive path based on a given strategy or custom selector.
/// </summary>
public sealed class PathEnumerator
{
    private readonly Func<string, string> _selector;
    private readonly string _searchPattern;
    private readonly string _directory;
    private readonly bool _recursive;

    /// <summary>
    /// Initializes a new instance of the <see cref="PathEnumerator"/> class with a predefined naming strategy
    /// to construct the archive paths.
    /// </summary>
    /// <param name="directory">
    /// The directory to iterate over.
    /// </param>
    /// <param name="searchPattern">
    /// A search pattern to limit the files to add. <c>"*"</c> will for example add all files, <c>"*.txt"</c>
    /// would only add files with the given extension.
    /// </param>
    /// <param name="recursive">
    /// <c>true</c> to include subdirectories, <c>false</c> to add the top-level files only.
    /// </param>
    /// <param name="strategy">
    /// The naming strategy to apply for each file that shall be added to the archive. See <see cref="NamingStrategy"/>
    /// for details.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any of the arguments is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="directory"/> or <paramref name="searchPattern"/> is an empty string.
    /// </exception>
    public PathEnumerator(string directory, string searchPattern, bool recursive, NamingStrategy strategy)
        : this(directory, searchPattern, recursive, NamingStrategySelector.Get(strategy, directory))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PathEnumerator"/> class with a custom selector
    /// to construct the archive paths.
    /// </summary>
    /// <param name="directory">
    /// The directory to iterate over.
    /// </param>
    /// <param name="searchPattern">
    /// A search pattern to limit the files to add. <c>"*"</c> will for example add all files, <c>"*.txt"</c>
    /// would only add files with the given extension.
    /// </param>
    /// <param name="recursive">
    /// <c>true</c> to include subdirectories, <c>false</c> to add the top-level files only.
    /// </param>
    /// <param name="selector">
    /// The callback used to construct the archive path from the file path, which is passed as argument.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any of the arguments is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="directory"/> or <paramref name="searchPattern"/> is an empty string.
    /// </exception>
    public PathEnumerator(string directory, string searchPattern, bool recursive, Func<string, string> selector)
    {
        _searchPattern = searchPattern ?? throw new ArgumentNullException(nameof(searchPattern));
        _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        _recursive = recursive;
        _selector = selector ?? throw new ArgumentNullException(nameof(selector));

        if (string.IsNullOrWhiteSpace(directory))
        { 
            throw new ArgumentException("The directory to iterate over must not be an empty string.", nameof(directory));
        }

        if (string.IsNullOrWhiteSpace(searchPattern))
        {
            throw new ArgumentException("The search pattern mut not be an empty string.", nameof(directory));
        }
    }

    /// <summary>
    /// Iterates over all files within the base directory and, if this instance was created with the
    /// <c>recursive</c> option, all subdirectories and transforms the file path to the archive path
    /// by using the selector passed to the constructor.
    /// </summary>
    /// <param name="error">
    /// An optional error handler which is invoked when an exception occurs while constructing the archive path. Any
    /// file whose archive path cannot be created will be skipped.
    /// </param>
    /// <returns>
    /// A lazy collection providing the path and the corresponding archive path.
    /// </returns>
    /// <exception cref="DirectoryNotFoundException">
    /// Thrown if the directory to iterate over does not exist.
    /// </exception>
    /// <exception cref="System.Security.SecurityException">
    /// Thrown if the directory or a file cannot be accessed.
    /// </exception>
    public IEnumerable<(string path, string archivePath)> EnumerateLazy(Action<string, Exception> error = null)
    {
        var searchOption = _recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        foreach (var path in Directory.EnumerateFiles(_directory, _searchPattern, searchOption))
        {
            var archivePath = default(string);
            try
            {
                archivePath = _selector(path);   
            }
            catch (Exception ex)
            {
                error?.Invoke(path, ex);
                continue;
            }

            yield return (path, archivePath);
        }
    }
}
