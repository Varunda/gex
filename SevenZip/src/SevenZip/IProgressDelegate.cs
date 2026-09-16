namespace SevenZip;

/// <summary>
/// The <see cref="IProgressDelegate"/> is used to track the progress of an extract or compress operation. It is
/// supported by the <see cref="IArchiveReaderDelegate"/> and <see cref="IArchiveWriterDelegate"/> interfaces.
/// </summary>
public interface IProgressDelegate<TContext> where TContext : IOperationContext
{
    /// <summary>
    /// Indicates the current progress of the extract operation.
    /// </summary>
    /// <param name="context">The context can provide additional information about the current operation.</param>
    /// <param name="current">The current progress value with a range of [0 and <paramref name="total"/>].</param>
    /// <param name="total">The value indicating progress completion.</param>
    void OnProgress(TContext context, ulong current, ulong total);

    /// <summary>
    /// Indicates that a new extract or compress operation has been initiated.
    /// </summary>
    /// <param name="context">The context can provide additional information about the current operation.</param>
    void OnProgressBegin(TContext context);

    /// <summary>
    /// Indicates that an extract or a compress operation has been initiated.
    /// </summary>
    /// <param name="context">The context can provide additional information about the current operation.</param>
    void OnProgressEnd(TContext context);
}