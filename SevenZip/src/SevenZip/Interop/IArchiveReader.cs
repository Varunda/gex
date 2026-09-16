using System.Runtime.InteropServices;

namespace SevenZip.Interop;

/// <summary>
/// The <see cref="IArchiveReader"/> interface is used to open an archive, read its properties and extract files.
/// </summary>
/// <remarks>
/// https://github.com/mcmilk/7-Zip/blob/826145b86107fc0a778ac673348226db180e4532/CPP/7zip/Archive/IArchive.h#L316
/// </remarks>
[Guid("23170F69-40C1-278A-0000-000600600000")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#if NET8_0_OR_GREATER
[System.Runtime.InteropServices.Marshalling.GeneratedComInterface]
partial
#else
[ComImport]
#endif
interface IArchiveReader
{
    /// <summary>
    /// Opens an archive represented by the given <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">The stream providing read-access to the archive.</param>
    /// <param name="maxCheckStartPosition">The maximum offset within the given stream to search for the beginning of the archive data.</param>
    /// <param name="callback">An optional callback interface.</param>
    /// <returns>Zero (0) if the operation succeeded.</returns>
    [PreserveSig]
    int Open(IInputStream stream, ref ulong maxCheckStartPosition, [MarshalAs(UnmanagedType.Interface)] IArchiveOpenCallback callback);

    /// <summary>
    /// Closes the currently opened stream.
    /// </summary>
    void Close();

    /// <summary>
    /// Gets the number of entries stored in the opened archive.
    /// </summary>
    /// <returns>The number of archive entries.</returns>
    /// <remarks>
    /// This method must only be called between <see cref="Open(IInputStream, ref ulong, IArchiveOpenCallback)"/>
    /// and <see cref="Close"/>.
    /// </remarks>
    uint GetNumberOfItems();
        
    /// <summary>
    /// Gets the property of the given archive entry.
    /// </summary>
    /// <param name="index">The index of the archive entry to read the property for.</param>
    /// <param name="property">The property type, see <see cref="ArchiveEntryProperty"/>.</param>
    /// <param name="value">A <see cref="Union"/> containing the value read.</param>
    /// <remarks>
    /// This method must only be called between <see cref="Open(IInputStream, ref ulong, IArchiveOpenCallback)"/>
    /// and <see cref="Close"/>.
    /// </remarks>
    void GetProperty(uint index, ArchiveEntryProperty property, ref Union value);

    /// <summary>
    /// Extracts the entries from the opened archive.
    /// </summary>
    /// <param name="indices">An array containing the archive entry indices to extract, or <c>null</c>, to extract all files.</param>
    /// <param name="length">The length of <paramref name="indices"/> or -1, if <paramref name="indices"/> is <c>null</c>.</param>
    /// <param name="testMode">Zero (0) to extract the files to an output stream or one (1), to perform a test run only.</param>
    /// <param name="extractCallback">The callback interface used to notify the operation status and to obtain output streams.</param>
    /// <returns>Zero (0) if the operation succeeded.</returns>
    /// <remarks>
    /// This method must only be called between <see cref="Open(IInputStream, ref ulong, IArchiveOpenCallback)"/>
    /// and <see cref="Close"/>.
    /// </remarks>
    [PreserveSig]
    int Extract(
        [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] uint[] indices,
        uint length,
        int testMode,
        [MarshalAs(UnmanagedType.Interface)] IArchiveExtractCallback extractCallback);
}