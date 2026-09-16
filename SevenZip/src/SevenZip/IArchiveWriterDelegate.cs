using System;

namespace SevenZip;

/// <summary>
/// The <see cref="IArchiveWriterDelegate"/> interface can be used to track to progress of compress operations.
/// It supports the <see cref="IProgressDelegate"/> interfaces and additionally receives notifications when
/// an input stream creation failed and when an archive entry has been processed.
/// </summary>
public interface IArchiveWriterDelegate : IProgressDelegate<ICompressContext>
{
    /// <summary>
    /// Called when the creation of an input stream for the given index failed.
    /// </summary>
    /// <param name="context">The context can provide additional information about the current operation.</param>
    /// <param name="index">The index of the archive entry whose stream could not be created.</param>
    /// <param name="path">The path of the entry within the archive.</param>
    /// <param name="ex">The exception object.</param>
    void OnGetStreamFailed(ICompressContext cntext, int index, string path, Exception ex);

    /// <summary>
    /// Called when an archive entry has been processed.
    /// </summary>
    /// <param name="context">The context can provide additional information about the current operation.</param>
    /// <param name="index">The index of the current <see cref="ArchiveEntry"/>.</param>
    /// <param name="path">The path of the entry within the archive.</param>
    /// <param name="result">The <see cref="OperationResult"/> indicating whether the entry
    /// could be written to the archive or not.</param>
    void OnCompressOperation(ICompressContext context, int index, string path, OperationResult result);
}