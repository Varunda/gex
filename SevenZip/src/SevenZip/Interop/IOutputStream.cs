using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SevenZip.Interop;

/// <summary>
/// The <see cref="IOutputStream"/> is used by an <see cref="IArchiveReader"/> to read the archive data.
///
/// <para>
/// COM-Interface inheritance is not supported in earlier .NET versions, so we have to redeclare inherited
/// members in that case.
/// </para>
/// </summary>
/// <remarks>https://github.com/mcmilk/7-Zip/blob/826145b86107fc0a778ac673348226db180e4532/CPP/7zip/IStream.h#L101</remarks>
[Guid("23170F69-40C1-278A-0000-000300040000")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#if NET8_0_OR_GREATER
[System.Runtime.InteropServices.Marshalling.GeneratedComInterface]
partial
#else
[ComImport]
#endif
interface IOutputStream  : ISequentialOutputStream
{
#if !NET8_0_OR_GREATER
    /// <summary>
    /// Write <paramref name="size"/> bytes from the given <paramref name="buffer"/> to the output stream.
    /// </summary>
    /// <param name="buffer">The buffer to copy the bytes from.</param>
    /// <param name="size">The number of bytes to copy.</param>
    /// <returns>The number of bytes written.</returns>
    new int Write(nint buffer, uint size);
#endif

    /// <summary>
    /// Sets the position of the stream.
    /// </summary>
    /// <param name="offset">The offset relative to the given <paramref name="origin"/>.</param>
    /// <param name="origin">The reference point from where to set the position.</param>
    /// <param name="newPositionPtr">A pointer to which to write the new stream position to.</param>
    /// <returns>A value of zero (0) on success, an error code otherwise.</returns>
    [PreserveSig]
    int Seek(long offset, SeekOrigin origin, IntPtr newPositionPtr);

    /// <summary>
    /// Sets the stream size.
    /// </summary>
    /// <param name="size">The new stream size.</param>
    /// <returns>A value of zero (0) on success, an error code otherwise.</returns>
    [PreserveSig]
    int SetSize(long size);
}