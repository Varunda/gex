using System;
using System.Runtime.InteropServices;

namespace SevenZip.Interop;

/// <summary>
/// The <see cref="IArchiveUpdateCallback"/> can be used to receive notifications when an archive is being created
/// or updated.
/// </summary>
/// <remarks>
/// The declaration of this COM interface is taken from
/// https://github.com/mcmilk/7-Zip/blob/826145b86107fc0a778ac673348226db180e4532/CPP/7zip/Archive/IArchive.h#L445
/// </remarks>
[Guid("23170F69-40C1-278A-0000-000600800000")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#if NET8_0_OR_GREATER
[System.Runtime.InteropServices.Marshalling.GeneratedComInterface(Options = System.Runtime.InteropServices.Marshalling.ComInterfaceOptions.ManagedObjectWrapper)]
partial
#else
[ComImport]
#endif
interface IArchiveUpdateCallback
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
    /// Retrieves information about a file within the archive.
    /// </summary>
    /// <param name="index">The file index (See <see cref="ArchiveEntry.Index"/>.</param>
    /// <param name="newData">Zero (0) to copy data from the archive, one (1) if new data is present.</param>
    /// <param name="newProperties">Zero (0) to copy properties from the archive, one (1) if new properties are present.</param>
    /// <param name="indexInArchive">The desired index the file should have within the archive or -1 to let the library decide.</param>
    /// <returns>A value of zero (0) on success, an error code otherwise.</returns>
    [PreserveSig]
    int GetUpdateItemInfo(uint index, ref int newData, ref int newProperties, ref uint indexInArchive);

    /// <summary>
    /// Gets the property of the given archive entry.
    /// </summary>
    /// <param name="index">The index of the archive entry to read the property for.</param>
    /// <param name="property">The property type, see <see cref="ArchiveEntryProperty"/>.</param>
    /// <param name="value">A <see cref="Union"/> containing the value read.</param>
    /// <returns>A value of zero (0) on success, an error code otherwise.</returns>
    [PreserveSig]
    int GetProperty(uint index, ArchiveEntryProperty property, ref Union value);

    /// <summary>
    /// Gets a read-stream for the given archive entry.
    /// </summary>
    /// <param name="index">The index of the archive entry to read.</param>
    /// <param name="reader">The associated input stream.</param>
    /// <returns>A value of zero (0) on success, an error code otherwise.</returns>
    [PreserveSig]
    int GetStream(
        uint index,
        [MarshalAs(UnmanagedType.Interface)] out ISequentialInputStream reader);

    /// <summary>
    /// Sets the operation result of the file entry currently being processed.
    /// </summary>
    /// <param name="operationResult">The <see cref="OperationResult"/> value.</param>
    void SetOperationResult(OperationResult operationResult);
}