namespace SevenZip;

/// <summary>
/// Enumeration listing the supported naming strategies to create an archive path
/// from a file path.
/// </summary>
public enum NamingStrategy
{
    /// <summary>
    /// All archive paths are relative to the parent-directory of the given root directory,
    /// i.e., they start with the name of the to root directory. The structure
    /// of any subdirectories remains.
    /// </summary>
    /// <example>
    /// When the root directory is <c>C:\root\dir\</c>, a path of <code>C:\root\dir\sub\file.txt</code>
    /// will result in <code>dir\sub\file.txt</code>.
    /// </example>
    RelativeToTopDirectoryInclusive,

    /// <summary>
    /// All archive paths are relative to the given root directory, i.e., the name of
    /// the root directory itself is not part of the path. The structure of any
    /// subdirectories remains.
    /// </summary>
    /// <example>
    /// When the root directory is <c>C:\root\dir\</c>, a path of <code>C:\root\dir\sub\file.txt</code>
    /// will result in <code>sub\file.txt</code>.
    /// </example>
    RelativeToTopDirectoryExclusive,

    /// <summary>
    /// All archive paths are reduced to the filenames.
    /// </summary>
    /// <example>
    /// When the root directory is <c>C:\root\dir\</c>, a path of <code>C:\root\dir\sub\file.txt</code>
    /// will result in <code>file.txt</code>.
    /// </example>
    FilenamesOnly,

    /// <summary>
    /// All archive paths will contain the entire path of the source file except the root element.
    /// </summary>
    /// <example>
    /// When the root directory is <c>C:\root\dir\</c>, a path of <code>C:\root\dir\sub\file.txt</code>
    /// will result in <code>root\dir\sub\file.txt</code>.
    /// </example>
    Absolute,
}