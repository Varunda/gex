using System;

namespace SevenZip;

/// <summary>
/// The <see cref="IArchiveReaderDelegate"/> can be used to receive status notifications while testing
/// or extracting an archive.
/// </summary>
public interface IArchiveReaderDelegate : IProgressDelegate<IExtractContext>
{
    /// <summary>
    /// Called by the <see cref="ArchiveReader"/> when an output stream for an extract
    /// operation could not be obtained or opened.
    /// </summary>
    /// <param name="context">The context can provide additional information about the current operation.</param>
    /// <param name="index">The index of the current <see cref="ArchiveEntry"/>.</param>
    /// <param name="entry">The current <see cref="ArchiveEntry"/> or <c>null</c>, if the entry does not exist.</param>
    /// <param name="ex">The exception that has been thrown.</param>
    void OnGetStreamFailed(IExtractContext context, int index, ArchiveEntry? entry, Exception ex);
        
    /// <summary>
    /// Called by the <see cref="ArchiveReader"/> when an extract operation for one <see cref="ArchiveEntry"/>
    /// completed or failed.
    /// </summary>
    /// <param name="context">The context can provide additional information about the current operation.</param>
    /// <param name="index">The index of the current <see cref="ArchiveEntry"/>.</param>
    /// <param name="entry">The current <see cref="ArchiveEntry"/> or <c>null</c>, if the entry does not exist.</param>
    /// <param name="operation">The operation that was performed for the given entry.</param>
    /// <param name="result">The <see cref="OperationResult"/> indicating whether the entry
    /// could be written to the archive or not.</param>
    void OnExtractOperation(IExtractContext context, int index, ArchiveEntry? entry, ExtractOperation operation, OperationResult result);
}