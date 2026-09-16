namespace SevenZip;

/// <summary>
/// The <see cref="ICompressContext"/> manages the internal state of extract operations and is
/// passed to the <see cref="IProgressDelegate{TContext}"/> and <see cref="IArchiveWriterDelegate"/>
/// interface to report the current state.
/// </summary>
public interface ICompressContext : IOperationContext
{
    /// <summary>
    /// Gets the associated writer.
    /// </summary>
    ArchiveWriter ArchiveWriter { get; }
}
