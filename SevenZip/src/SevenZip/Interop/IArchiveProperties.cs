using System;
using System.Runtime.InteropServices;

namespace SevenZip.Interop;

/// <summary>
/// The <see cref="IArchiveProperties"/> interface is used to configure the native library,
/// for example when compressing data.
/// See <see cref="CompressProperties"/> for more information.
/// </summary>
/// <remarks>
/// https://github.com/mcmilk/7-Zip/blob/826145b86107fc0a778ac673348226db180e4532/CPP/7zip/Archive/IArchive.h#L537
/// </remarks>
[Guid("23170F69-40C1-278A-0000-000600030000")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#if NET8_0_OR_GREATER
[System.Runtime.InteropServices.Marshalling.GeneratedComInterface]
partial
#else
[ComImport]
#endif
interface IArchiveProperties
{
    /// <summary>
    /// Forwards the properties to the native library. Any allocated memory must be freed
    /// by the caller once the call returns.
    /// </summary>
    /// <param name="names">
    /// A pointer to an array of binary strings containing the option names.
    /// </param>
    /// <param name="values">
    /// A pointer to an array of <see cref="Union"/>s containing the option values.
    /// </param>
    /// <param name="count">
    /// The number of options.
    /// </param>
    /// <returns>
    /// <returns>Zero (0) if the operation succeeded.</returns>
    /// </returns>
    [PreserveSig]
    int SetProperties(IntPtr names, IntPtr values, int count);
}