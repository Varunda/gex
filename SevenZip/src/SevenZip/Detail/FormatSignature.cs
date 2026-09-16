using System;
using System.Collections.Generic;
using System.Linq;

namespace SevenZip.Detail;

/// <summary>
/// Representation of a known archive format signature.
/// </summary>
/// <remarks>
/// Most signatures can be found on the following page: https://en.wikipedia.org/wiki/List_of_file_signatures
/// </remarks>
internal readonly struct FormatSignature
{
    private readonly byte[] _signature;

    /// <summary>
    /// Initializes a new instance of the <see cref="FormatSignature"/> struct.
    /// </summary>
    /// <param name="format">The format to register a signature for.</param>
    /// <param name="signature">The bytes indicating the given <paramref name="format"/>.</param>
    public FormatSignature(ArchiveFormat format, params byte[] signature) => (Format, _signature) = (format, signature);

    /// <summary>
    /// Gets the <see cref="ArchiveFormat"/> of this instance.
    /// </summary>
    public ArchiveFormat Format { get; }

    /// <summary>
    /// Gets the length of the format's signature, in bytes.
    /// </summary>
    public int SignatureLength => _signature.Length;

    /// <summary>
    /// Gets a static list containing all known signatures.
    /// </summary>
    public static IReadOnlyList<FormatSignature> KnownSignatures { get; } = CreateSignatureList();

    /// <summary>
    /// Tests if the given <paramref name="buffer"/> starts with the signature of this instance.
    /// </summary>
    /// <param name="buffer">The buffer whose content shall be compared against the signature of this format.</param>
    /// <returns><c>true</c>, if the signature was found. Otherwise <c>false</c>.</returns>
    public bool StartsWithSignature(Span<byte> buffer)
    {
        if (SignatureLength >= buffer.Length)
        {
            return false;
        }

        for (var i = 0; i < SignatureLength; ++i)
        {
            if (buffer[i] != _signature[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Searches the given <paramref name="buffer"/> for the signature of this instance.
    /// </summary>
    /// <param name="buffer">The buffer to iterate over to find the format's signature.</param>
    /// <returns>The index where the format's signature was found or -1, if the signature was not found.</returns>
    public int FindSignature(Span<byte> buffer)
    {
        if (SignatureLength < 1)
        {
            return -1;
        }

        var head = _signature[0];
        var length = buffer.Length - SignatureLength;

        for (var offset = 0; offset < length; ++offset)
        {
            offset = buffer.Slice(offset).IndexOf(head);

            if (offset == -1)
            {
                return -1;
            }

            if (StartsWithSignature(buffer.Slice(offset)))
            {
                return offset;
            }
        }

        return -1;
    }

    /// <summary>
    /// Gets the <see cref="FormatSignature"/> for the given <paramref name="format"/>.
    /// </summary>
    /// <param name="format">The format to retrieve the signature for.</param>
    /// <returns>The corresponding signature.</returns>
    public static FormatSignature Get(ArchiveFormat format)
    {
        return KnownSignatures.FirstOrDefault(signature => signature.Format == format);
    }

    private static IReadOnlyList<FormatSignature> CreateSignatureList()
    {
        return new FormatSignature[]
        {
            new(ArchiveFormat.SevenZip, 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C),
            new(ArchiveFormat.GZip, 0x1F, 0x8B, 0x08),
            new(ArchiveFormat.Tar, 0x75, 0x73, 0x74, 0x61, 0x72),
            new(ArchiveFormat.Rar, 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x00),
            new(ArchiveFormat.Zip, 0x50, 0x4B, 0x03, 0x04),
            new(ArchiveFormat.Lzma, 0x5D, 0x00, 0x00, 0x40, 0x00),
            new(ArchiveFormat.Lzh, 0x2D, 0x6C, 0x68),
            new(ArchiveFormat.Lzw, 0x1F, 0x9D, 0x90),
            new(ArchiveFormat.Arj, 0x60, 0xEA),
            new(ArchiveFormat.BZip2, 0x42, 0x5A ,0x68),
            new(ArchiveFormat.Cab, 0x4D, 0x53, 0x43, 0x46),
            new(ArchiveFormat.Chm, 0x49, 0x54, 0x53, 0x46),
            new(ArchiveFormat.Deb, 0x21, 0x3C, 0x61, 0x72, 0x63, 0x68, 0x3E, 0x0A, 0x64, 0x65, 0x62, 0x69, 0x61, 0x6E, 0x2D, 0x62, 0x69, 0x6E, 0x61, 0x72, 0x79),
            new(ArchiveFormat.Iso, 0x43, 0x44, 0x30, 0x30, 0x31),
            new(ArchiveFormat.Rpm, 0xED, 0xAB, 0xEE, 0xDB),
            new(ArchiveFormat.Wim, 0x4D, 0x53, 0x57, 0x49, 0x4D, 0x00, 0x00, 0x00),
            new(ArchiveFormat.Xar, 0x78, 0x61, 0x72, 0x21),
            new(ArchiveFormat.Hfs, 0x48, 0x2B),
            new(ArchiveFormat.Xz, 0xFD, 0x37, 0x7A, 0x58, 0x5A),
            new(ArchiveFormat.Flv, 0x46, 0x4C, 0x56),
            new(ArchiveFormat.Swf, 0x46, 0x57, 0x53),
            new(ArchiveFormat.Pe, 0x4D, 0x5A),
            new(ArchiveFormat.Elf, 0x7F, 0x45, 0x4C, 0x46),
            new(ArchiveFormat.Dmg, 0x78),
            new(ArchiveFormat.Vhd, 0x63, 0x6F, 0x6E, 0x65, 0x63, 0x74, 0x69, 0x78)
        };
    }
}