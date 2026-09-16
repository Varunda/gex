using System;

namespace SevenZip;

/// <summary>
/// A default implementation of the <see cref="IArchiveReaderDelegate"/> interface which
/// accepts lambda expressions as callbacks.
/// </summary>
public sealed class ArchiveReaderDelegate : IArchiveReaderDelegate
{
    private readonly Action<IExtractContext, int, ArchiveEntry?, Exception> _onGetStreamFailed;
    private readonly Action<IExtractContext, int, ArchiveEntry?, ExtractOperation, OperationResult> _onExtractOperation;
    private readonly Action<IExtractContext, ulong, ulong> _onProgress;
    private readonly Action<IExtractContext> _onProgressBegin;
    private readonly Action<IExtractContext> _onProgressEnd;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveReaderDelegate"/> class.
    /// </summary>
    /// <param name="onGetStreamFailed">This callback is invoked when an output stream
    /// could not be obtained. May be <c>null</c>.</param>
    /// <param name="onExtractOperation">This callback is invoked when the status of
    /// an extract operation changed. May be <c>null</c>.</param>
    /// <param name="onProgress">This callback is invoked when progress is being reported.</param>
    /// <param name="onProgressBegin">This callback is invoked when a new extract or compress operation has been initiated.</param>
    /// <param name="onProgressEnd">This callback is invoked when an extract or a compress operation has completed.</param>
    public ArchiveReaderDelegate(
        Action<IExtractContext, int, ArchiveEntry?, Exception> onGetStreamFailed = null,
        Action<IExtractContext, int, ArchiveEntry?, ExtractOperation, OperationResult> onExtractOperation = null,
        Action<IExtractContext, ulong, ulong> onProgress = null,
        Action<IExtractContext> onProgressBegin = null,
        Action<IExtractContext> onProgressEnd = null)
    {
        _onGetStreamFailed = onGetStreamFailed;
        _onExtractOperation = onExtractOperation;
        _onProgress = onProgress;
        _onProgressBegin = onProgressBegin;
        _onProgressEnd = onProgressEnd;
    }

    internal static IArchiveReaderDelegate Default { get; } = new ArchiveReaderDelegate();

    #region IProgressDelegate
    void IProgressDelegate<IExtractContext>.OnProgress(IExtractContext context, ulong current, ulong total) =>
        _onProgress?.Invoke(context, current, total);

    void IProgressDelegate<IExtractContext>.OnProgressBegin(IExtractContext context) =>
        _onProgressBegin?.Invoke(context);

    void IProgressDelegate<IExtractContext>.OnProgressEnd(IExtractContext context) =>
        _onProgressEnd?.Invoke(context);
    #endregion

    #region IArchiveReaderDelegate
    void IArchiveReaderDelegate.OnGetStreamFailed(IExtractContext context, int index, ArchiveEntry? entry, Exception ex) => 
        _onGetStreamFailed?.Invoke(context, index, entry, ex);

    void IArchiveReaderDelegate.OnExtractOperation(IExtractContext context, int index, ArchiveEntry? entry, ExtractOperation operation, OperationResult result) =>
        _onExtractOperation?.Invoke(context, index, entry, operation, result);
    #endregion
}
