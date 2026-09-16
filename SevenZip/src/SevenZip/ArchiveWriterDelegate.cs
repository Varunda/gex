using System;

namespace SevenZip;

/// <summary>
/// A default implementation of the <see cref="IArchiveWriterDelegate"/> interface which
/// accepts lambda expressions as callbacks.
/// </summary>
public sealed class ArchiveWriterDelegate : IArchiveWriterDelegate
{
    private readonly Action<ICompressContext, int, string, Exception> _onGetStreamFailed;
    private readonly Action<ICompressContext, int, string, OperationResult> _onCompressOperation;
    private readonly Action<ICompressContext, ulong, ulong> _onProgress;
    private readonly Action<ICompressContext> _onProgressBegin;
    private readonly Action<ICompressContext> _onProgressEnd;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveWriterDelegate"/> class.
    /// </summary>
    /// <param name="onGetStreamFailed">
    /// This callback is invoked when when input stream could not be created.
    /// The first parameter contains the archive entry index, the second one 
    /// contains the archive path and the third one the exception.
    /// </param>
    /// <param name="onCompressOperation">
    /// This callback is invoked when a compress operation completed.
    /// The first parameter contains the archive entry index, the second one 
    /// contains the archive path and the third one the <see cref="OperationResult"/>.
    /// </param>
    /// <param name="onProgress">
    /// This callback is invoked when a progress is being reported.
    /// </param>
    /// <param name="onProgressBegin">This callback is invoked when a new extract or compress operation has been initiated.</param>
    /// <param name="onProgressEnd">This callback is invoked when an extract or a compress operation has completed.</param>
    public ArchiveWriterDelegate(
        Action<ICompressContext, int, string, Exception> onGetStreamFailed = null,
        Action<ICompressContext, int, string, OperationResult> onCompressOperation = null,
        Action<ICompressContext, ulong, ulong> onProgress = null,
        Action<ICompressContext> onProgressBegin = null,
        Action<ICompressContext> onProgressEnd = null)
    {
        _onGetStreamFailed = onGetStreamFailed;
        _onCompressOperation = onCompressOperation;
        _onProgress = onProgress;
        _onProgressBegin = onProgressBegin;
        _onProgressEnd = onProgressEnd;
    }

    internal static IArchiveWriterDelegate Default { get; } = new ArchiveWriterDelegate();

    #region IProgressDelegate
    void IProgressDelegate<ICompressContext>.OnProgress(ICompressContext context, ulong current, ulong total) =>
        _onProgress?.Invoke(context, current, total);

    void IProgressDelegate<ICompressContext>.OnProgressBegin(ICompressContext context) =>
        _onProgressBegin?.Invoke(context);

    void IProgressDelegate<ICompressContext>.OnProgressEnd(ICompressContext context) =>
        _onProgressEnd?.Invoke(context);
    #endregion

    #region IArchiveWriterDelegate
    void IArchiveWriterDelegate.OnGetStreamFailed(ICompressContext context, int index, string path, Exception ex) =>
        _onGetStreamFailed?.Invoke(context, index, path, ex);

    void IArchiveWriterDelegate.OnCompressOperation(ICompressContext context, int index, string path, OperationResult result) =>
        _onCompressOperation?.Invoke(context, index, path, result);
    #endregion
}
