namespace SevenZip;

/// <summary>
/// Enumeration listing the supported compression methods.
/// </summary>
/// <remarks>
/// The compression method is configured via the archive property <c>m</c>
/// for Zip archives and <c>0</c> for all others. Not all methods are
/// supported by all archive types.
/// </remarks>
public enum CompressionMethod
{
    /// <summary>
    /// Zip or 7-zip, no compression.
    /// </summary>
    Copy,

    /// <summary>
    /// Zip, deflate.
    /// </summary>
    Deflate,

    /// <summary>
    /// Zip, deflate64.
    /// </summary>
    Deflate64,
    
    /// <summary>
    /// Zip or 7-zip, <a href="http://en.wikipedia.org/wiki/Cabinet_(file_format)">BZip2</a>.
    /// </summary>
    /// <remarks></remarks>
    BZip2,

    /// <summary>
    /// Zip or 7-zip, LZMA method based on Lempel-Ziv algorithm, default for 7-zip.
    /// </summary>
    Lzma,

    /// <summary>
    /// 7-zip, LZMA version 2, LZMA with improved multithreading and usually slight archive size decrease.
    /// </summary>
    Lzma2,

    /// <summary>
    /// Zip or 7-zip, <a href="http://en.wikipedia.org/wiki/Prediction_by_Partial_Matching">PPMd</a> based on Dmitry Shkarin's PPMdH source code, very efficient for compressing texts.
    /// </summary>
    Ppmd,

    /// <summary>
    /// No method change.
    /// </summary>
    Default
}
