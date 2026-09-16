using System;
using System.Runtime.InteropServices;

namespace SevenZip.Interop;

/// <summary>
/// The <see cref="IArchiveOpenCallback"/> can be used to receive notifications when an archive is being opened.
///
/// This interface is not used for all archive formats.
/// </summary>
/// <remarks>
/// The declaration of this COM interface is taken from
/// https://github.com/mcmilk/7-Zip/blob/826145b86107fc0a778ac673348226db180e4532/CPP/7zip/Archive/IArchive.h#L177
/// </remarks>
[Guid("23170F69-40C1-278A-0000-000600100000")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#if NET8_0_OR_GREATER
[System.Runtime.InteropServices.Marshalling.GeneratedComInterface(Options = System.Runtime.InteropServices.Marshalling.ComInterfaceOptions.ManagedObjectWrapper)]
partial
#else
[ComImport]
#endif
interface IArchiveOpenCallback
{
    /// <summary>
    /// Provides a pointer to the total number of files to extract and the archive size.
    /// </summary>
    /// <param name="files">A pointer to a 64 bit value providing the total number of files.</param>
    /// <param name="bytes">A pointer to a 64 bit value providing the archive size.</param>
    void SetTotal(IntPtr files, IntPtr bytes);

    /// <summary>
    /// Provides a pointer to the currently processed number of files and bytes.
    /// </summary>
    /// <param name="files">A pointer to a 64 bit value providing the current number of processed files.</param>
    /// <param name="bytes">A pointer to a 64 bit value providing the number of bytes that were already read.</param>
    void SetCompleted(IntPtr files, IntPtr bytes);
}