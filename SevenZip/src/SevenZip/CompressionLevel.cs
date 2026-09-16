namespace SevenZip;

/// <summary>
/// Enumeration listing the supported compressions levels. Depending on the
/// <see cref="ArchiveFormat"/>, the value might be ignored.
/// </summary>
public enum CompressionLevel
{
    /// <summary>
    /// No compression at all.
    /// </summary>
    None = 0,

    /// <summary>
    /// Lowest but fastest compression.
    /// </summary>
    Fast,

    /// <summary>
    /// Low compression.
    /// </summary>
    Low,

    /// <summary>
    /// Default compression.
    /// </summary>
    Normal,

    /// <summary>
    /// High compression.
    /// </summary>
    High,

    /// <summary>
    /// Hightest but also slowest compression.
    /// </summary>
    Ultra,
}