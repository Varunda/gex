using System.Runtime.InteropServices;

namespace SevenZip.Interop;

/// <summary>
/// The <see cref="ISequentialInputStream"/> is used by an <see cref="IArchiveReader"/> to read data.
///
/// This is interface corresponds to <c>ISequencialInStream</c> in the native 7z C++ library.
/// 
/// <para>
/// This interface ONLY provides the <c>Read</c> method when building for .NET 8 or later. In earlier
/// .NET versions, the <c>Read</c> method <c>MUST</c> be part of <c>IInputStream</c>. Otherwise,
/// an access violation will occur at runtime.
/// </para>
/// </summary>
/// <remarks>https://github.com/mcmilk/7-Zip/blob/826145b86107fc0a778ac673348226db180e4532/CPP/7zip/IStream.h#L97</remarks>
[Guid("23170F69-40C1-278A-0000-000300010000")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#if NET8_0_OR_GREATER
[System.Runtime.InteropServices.Marshalling.GeneratedComInterface]
partial
#else
[ComImport]
#endif
interface ISequentialInputStream
{
    /// <summary>
    /// Read up to <paramref name="size"/> bytes and copy them in the given <paramref name="buffer"/>.
    /// </summary>
    /// <param name="buffer">The array to copy the bytes read from the stream to.</param>
    /// <param name="size">The number of bytes to read.</param>
    /// <returns>The number of bytes actually read.</returns>
    int Read(nint buffer, uint size);
}