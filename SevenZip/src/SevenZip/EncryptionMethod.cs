namespace SevenZip;

/// <summary>
/// Enumeration listing the supported encryption methods.
/// </summary>
/// <remarks>
/// The encryption method is configured via the archive property <c>em</c>.
/// </remarks>
public enum EncryptionMethod
{
    /// <summary>
    /// ZipCrypto method, the weakest method, but supported by most libraries.
    /// </summary>
    ZipCrypto,

    /// <summary>
    /// AES-128 encryption.
    /// </summary>
    Aes128,

    /// <summary>
    /// AES-192 encryption.
    /// </summary>
    Aes192,

    /// <summary>
    /// AES-256 encryption.
    /// </summary>
    Aes256,
}
