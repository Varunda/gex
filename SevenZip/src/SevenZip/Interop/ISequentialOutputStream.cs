using System.Runtime.InteropServices;

namespace SevenZip.Interop;

/// <summary>
/// The <see cref="ISequentialOutputStream"/> is used to write the decompressed data of an archive. Usually, there
/// is one stream per file stored within an archive.
///
/// Instances of <see cref="ISequentialOutputStream"/> need to be created by the <see cref="IArchiveExtractCallback"/>
/// interface.
/// </summary>
/// <remarks>
/// https://github.com/mcmilk/7-Zip/blob/826145b86107fc0a778ac673348226db180e4532/CPP/7zip/IStream.h#L101
/// </remarks>
[Guid("23170F69-40C1-278A-0000-000300020000")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#if NET8_0_OR_GREATER
[System.Runtime.InteropServices.Marshalling.GeneratedComInterface]
partial
#else
[ComImport]
#endif
interface ISequentialOutputStream
{
    /// <summary>
    /// Write <paramref name="size"/> bytes from the given <paramref name="buffer"/> to the output stream.
    /// </summary>
    /// <param name="buffer">The buffer to copy the bytes from.</param>
    /// <param name="size">The number of bytes to copy.</param>
    /// <returns>The number of bytes written.</returns>
    int Write(nint buffer, uint size);
}