namespace SevenZip;

/// <summary>
/// The <see cref="IExtractContext"/> manages the internal state of extract operations and is
/// passed to the <see cref="IProgressDelegate{TContext}"/> and <see cref="IArchiveReaderDelegate"/>
/// interface to report the current state.
/// </summary>
public interface IExtractContext : IOperationContext
{
    /// <summary>
    /// Gets the associated reader.
    /// </summary>
    ArchiveReader ArchiveReader { get; }
}
