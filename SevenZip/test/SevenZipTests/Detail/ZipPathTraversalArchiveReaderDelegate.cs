using System;
using SevenZip;

namespace SevenZipTests.Detail;

internal sealed class ZipPathTraversalArchiveReaderDelegate : IArchiveReaderDelegate
{
    public void OnGetStreamFailed(IExtractContext context, int index, ArchiveEntry? entry, Exception ex)
    {
        throw ex;
    }

    public void OnExtractOperation(IExtractContext context, int index, ArchiveEntry? entry, ExtractOperation operation, OperationResult result)
    {
    }

    public void OnProgress(IExtractContext context, ulong current, ulong total)
    {
    }

    public void OnProgressBegin(IExtractContext context)
    {
    }

    public void OnProgressEnd(IExtractContext context)
    {
    }
}
