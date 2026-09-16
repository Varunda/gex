using System.Runtime.InteropServices;

namespace SevenZip.Interop;

/// <summary>
/// The <see cref="IArchiveExtractCallback"/> is required when openening an archive to read its content
/// and extract files.
/// </summary>
/// <remarks>
/// The declaration of this COM interface is taken from
/// https://github.com/mcmilk/7-Zip/blob/826145b86107fc0a778ac673348226db180e4532/CPP/7zip/Archive/IArchive.h#L183
/// </remarks>
[Guid("23170F69-40C1-278A-0000-000600200000")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#if NET8_0_OR_GREATER
[System.Runtime.InteropServices.Marshalling.GeneratedComInterface(Options = System.Runtime.InteropServices.Marshalling.ComInterfaceOptions.ManagedObjectWrapper)]
partial
#else
[ComImport]
#endif
interface IArchiveExtractCallback
{
    /// <summary>
    /// Indicates the overall progress of an extract operation.
    /// </summary>
    /// <param name="total">Unused</param>
    void SetTotal(ulong total);

    /// <summary>
    /// Indicates the progress.
    /// </summary>
    /// <param name="completeValue">Unused</param>
    void SetCompleted(
#if !NET8_0_OR_GREATER
        [In]
#endif
        ref ulong completeValue);

    /// <summary>
    /// The decompressor requests access to a writeable stream (<see cref="ISequentialOutputStream"/>) for the <see cref="ArchiveEntry"/> with
    /// the given <paramref name="index"/>.
    /// When the <paramref name="operation"/> is <see cref="ExtractOperation.Extract"/> and the given entry is a file, the implementation
    /// should create a valid output stream (e.g. a FileStream or a MemoryStream) and assign it to the <paramref name="writer"/> parameter.
    /// When assigning <c>null</c>, the entry will be skipped.
    /// </summary>
    /// <param name="index">The index of the archive the decompressor wants to extract.</param>
    /// <param name="writer">The pointer to assign the <see cref="ISequentialOutputStream"/> to.</param>
    /// <param name="operation">The current <see cref="ExtractOperation"/>.</param>
    /// <returns>A return value of 0 indicates success. In case of a non-zero value, the decompressor will cancel the operation.</returns>
    [PreserveSig]
    int GetStream(
        uint index,
        [MarshalAs(UnmanagedType.Interface)] out ISequentialOutputStream writer,
        ExtractOperation operation);

    /// <summary>
    /// The decompressor calls this method directly after <see cref="GetStream(uint, out ISequentialOutputStream, ExtractOperation)"/> to indicate
    /// that it starts the operation.
    /// </summary>
    /// <param name="operation">The operation being executed for the current entry.</param>
    void PrepareOperation(ExtractOperation operation);

    /// <summary>
    /// The decompressor calls this method to indicate the result of the operation that was initiated with a call to <see cref="GetStream(uint, out ISequentialOutputStream, ExtractOperation)"/>.
    ///
    /// Resources create within <see cref="GetStream(uint, out ISequentialOutputStream, ExtractOperation)"/> should now be freed again to avoid any kind of leaks or high memory usage.
    /// </summary>
    /// <param name="result">The operation result.</param>
    void SetOperationResult(OperationResult result);
}