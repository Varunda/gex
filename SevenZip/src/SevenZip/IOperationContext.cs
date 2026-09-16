namespace SevenZip;

/// <summary>
/// Interface to provide additional information about extract or compress operations reported by
/// interfaces like <see cref="IProgressDelegate{TContext}"/>, <see cref="IArchiveReaderDelegate"/> and
/// <see cref="IArchiveWriterDelegate"/>.
/// </summary>
/// <remarks>
/// Currently only used as common basis for <see cref="IExtractContext"/> and <see cref="ICompressContext"/>,
/// but common properties may
/// </remarks>
public interface IOperationContext
{
}
