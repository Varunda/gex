using System;
using System.Runtime.InteropServices;

namespace SevenZip.Interop;

/// <summary>
/// The <see cref="IArchiveWriter"/> is used to open an archive for write operations.
/// </summary>
/// <remarks>https://github.com/mcmilk/7-Zip/blob/826145b86107fc0a778ac673348226db180e4532/CPP/7zip/Archive/IArchive.h#L530</remarks>
[Guid("23170F69-40C1-278A-0000-000600A00000")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#if NET8_0_OR_GREATER
    [System.Runtime.InteropServices.Marshalling.GeneratedComInterface]
    partial
#else
[ComImport]
#endif
interface IArchiveWriter
{
    /// <summary>
    /// Writes all items to the given <paramref name="stream"/>.
    /// 
    /// The archive entries are obtained by invocations to the <see cref="IArchiveUpdateCallback"/>
    /// interface, which provides the item properties and their data.
    /// </summary>
    /// <param name="stream">
    /// The stream to write the archive data to.
    /// </param>
    /// <param name="itemCount">
    /// The total number of items to write. This is the sum of existing and new items.
    /// </param>
    /// <param name="callback">
    /// This instance is used to obtain the item properties and their uncompressed data.
    /// </param>
    /// <returns>
    /// A value of zero (0) on success, an error code otherwise.
    /// </returns>
    [PreserveSig]
    int UpdateItems(
        [MarshalAs(UnmanagedType.Interface)] ISequentialOutputStream stream,
        uint itemCount,
        [MarshalAs(UnmanagedType.Interface)] IArchiveUpdateCallback callback);
}