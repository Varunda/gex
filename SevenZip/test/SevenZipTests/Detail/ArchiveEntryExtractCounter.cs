using System;
using SevenZip;

namespace SevenZipTests.Detail;

internal sealed class ArchiveEntryExtractCounter : IArchiveReaderDelegate
{
    private long _counter;

    public long Result => _counter;

    void IProgressDelegate<IExtractContext>.OnProgress(IExtractContext context, ulong current, ulong total)
    {
    }

    void IProgressDelegate<IExtractContext>.OnProgressBegin(IExtractContext context)
    {
    }

    void IProgressDelegate<IExtractContext>.OnProgressEnd(IExtractContext context)
    {
    }

    void IArchiveReaderDelegate.OnGetStreamFailed(IExtractContext context, int index, ArchiveEntry? entry, Exception ex)
    {
        throw new ApplicationException($"Failed to create stream for index {index}.");
    }

    void IArchiveReaderDelegate.OnExtractOperation(IExtractContext context, int index, ArchiveEntry? entry, ExtractOperation operation, OperationResult result)
    {
        _counter++;
    }
}
