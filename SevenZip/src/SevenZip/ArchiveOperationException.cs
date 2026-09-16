using System;

namespace SevenZip;

/// <summary>
/// The <see cref="ArchiveOperationException"/> represents an error that occurred while processing an archive entry.
/// </summary>
public class ArchiveOperationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveOperationException"/> class.
    /// </summary>
    /// <param name="message">A textual description of the error.</param>
    /// <param name="result">The <see cref="OperationResult"/> indicating the kind of error.</param>
    /// <param name="entry">If available, the <see cref="ArchiveEntry"/> whose operation failed.</param>
    /// <param name="innerException">The exception that is the cause of this exception, if available.</param>
    internal ArchiveOperationException(string message, OperationResult result, ArchiveEntry? entry = null, Exception innerException = null)
        : base(message, innerException)
    {
        OperationResult = result;
        Index = entry?.Index;
        Path = entry?.Path;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveOperationException"/> class.
    /// </summary>
    /// <param name="message">A textual description of the error.</param>
    /// <param name="result">The <see cref="OperationResult"/> indicating the kind of error.</param>
    /// <param name="index">If available, the index of the entry whose operation failed.</param>
    /// <param name="path">If available, the archive path of the entry whose operation failed.</param>
    /// <param name="innerException">The exception that is the cause of this exception, if available.</param>
    internal ArchiveOperationException(string message, OperationResult result, int? index = null, string path = null, Exception innerException = null)
        : base(message, innerException)
    {
        OperationResult = result;
        Index = index;
        Path = path;
    }

    /// <summary>
    /// Gets the operation result indicating what went wrong.
    /// </summary>
    public OperationResult OperationResult { get; }

    /// <summary>
    /// Gets the index of the archive entry whose operation failed.
    /// </summary>
    public int? Index { get; }

    /// <summary>
    /// Gets the archive path of the entry whose operation failed or <c>null</c>.
    /// </summary>
    public string Path { get; }
}
