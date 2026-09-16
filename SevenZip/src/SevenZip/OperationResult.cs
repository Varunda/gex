namespace SevenZip;

/// <summary>
/// Enumeration listing the supported operation result types.
/// </summary>
public enum OperationResult
{
    /// <summary>
    /// The operation succeeded.
    /// </summary>
    Ok,

    /// <summary>
    /// The archive entry is compressed with an unsupported compression method (See <see cref="ArchiveEntry.Method"/>).
    /// </summary>
    UnsupportedMethod,

    /// <summary>
    /// A data related error occurred, this may happen when a stream cannot be read or written.
    /// </summary>
    DataError,

    /// <summary>
    /// A CRC error occurred.
    /// </summary>
    CrcError,

    /// <summary>
    /// An output stream is not available.
    /// </summary>
    Unavailable,

    /// <summary>
    /// The archive could not be read completely.
    /// </summary>
    UnexpectedEnd,

    /// <summary>
    /// Data was read after the end of a stream.
    /// </summary>
    DataAfterEnd,

    /// <summary>
    /// The input stream does not represent a supported or valid archive.
    /// </summary>
    IsNotArc,

    /// <summary>
    /// The archive headers could not be read.
    /// </summary>
    HeadersError,

    /// <summary>
    /// The provided password is incorrect.
    /// </summary>
    WrongPassword
}