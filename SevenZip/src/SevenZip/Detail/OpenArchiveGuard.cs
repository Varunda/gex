using System;
using System.IO;
using SevenZip.Interop;

namespace SevenZip.Detail;

/// <summary>
/// The <see cref="OpenArchiveGuard"/> struct opens an archive to read its data
/// or to extract files. When an instance of this type is being disposed, it
/// closes the archive again.
/// </summary>
internal readonly struct OpenArchiveGuard : IDisposable
{
    private readonly IArchiveReader _reader;
    private readonly ArchiveStream _stream;
    private readonly int _result;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenArchiveGuard"/> struct.
    /// </summary>
    /// <param name="reader">The <see cref="IArchiveReader"/> to use for the
    /// open operation.</param>
    /// <param name="baseStream">The stream providing the archive data. This stream
    /// will be reset to its beginning.</param>
    public OpenArchiveGuard(IArchiveReader reader, Stream baseStream)
    {
        var offset = 32768UL;

        _stream = new ArchiveStream(baseStream, leaveOpen: true);
        _stream.BaseStream.Seek(0, SeekOrigin.Begin);
        _reader = reader;
        _result = _reader.Open(_stream, ref offset, null);
    }

    /// <summary>
    /// Gets a value indicating the result of the open operation. Further archive
    /// operation must only be made if the status is <see cref="OperationResult.Ok"/>.
    /// </summary>
    public OperationResult OpenStatus => (OperationResult)_result;

    /// <summary>
    /// Tests if the <see cref="OpenStatus"/> is <see cref="OperationResult.Ok"/> or
    /// throws an <see cref="InvalidOperationException"/>, if not.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the archive could not be opened.</exception>
    public void EnsureOpened()
    {
        if (OpenStatus != OperationResult.Ok)
        {
            throw new InvalidOperationException($"The archive could not be opened: {OpenStatus}");
        }
    }
        
    /// <summary>
    /// Closes the archive again.
    /// </summary>
    public void Dispose()
    {
        _stream.Dispose();
        _reader.Close();
    }
}