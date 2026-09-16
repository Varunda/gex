using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SevenZip.Detail;

/// <summary>
/// This class provides helper functions that are related to the format of an archive.
/// </summary>
internal static class Format
{
    /// <summary>
    /// Detects the <see cref="ArchiveFormat"/> of the given <paramref name="stream"/> and resets the
    /// given <paramref name="stream"/> to its original position afterwards.
    /// This method seeks to the beginning of the stream and may search other regions within the stream
    /// to find a known archive signature. The callee must store the position in case he wants to reset the stream to
    /// the original position afterwards.
    /// </summary>
    /// <param name="stream">The stream to archive whose format shall be detected.</param>
    /// <param name="offset">Contains the offset where a known signature was found when this method returns.</param>
    /// <param name="isExecutable">Contains a value indicating whether the archive is an executable.</param>
    /// <returns>The detected <see cref="ArchiveFormat"/>.</returns>
    /// <exception cref="ArgumentException">Thrown if the given <paramref name="stream"/> cannot be read
    /// or contains less than 16 bytes.</exception>
    /// <exception cref="NotSupportedException">Thrown if the archive format could not be detected.</exception>
    public static ArchiveFormat DetectAndRestoreStreamPosition(Stream stream, out int offset, out bool isExecutable)
    {
        var position = stream.Position;
        var format = Detect(stream, out offset, out isExecutable);

        stream.Position = position;
        return format;
    }

    /// <summary>
    /// Detects the <see cref="ArchiveFormat"/> of the given <paramref name="stream"/>. This method seeks
    /// to the beginning of the stream and may search other regions within the stream to find a known
    /// archive signature. The callee must store the position in case he wants to reset the stream to
    /// the original position afterwards.
    /// </summary>
    /// <param name="stream">The stream to archive whose format shall be detected.</param>
    /// <param name="offset">Contains the offset where a known signature was found when this method returns.</param>
    /// <param name="isExecutable">Contains a value indicating whether the archive is an executable.</param>
    /// <returns>The detected <see cref="ArchiveFormat"/>.</returns>
    /// <exception cref="NotSupportedException">Thrown if the archive format could not be detected.</exception>
    public static unsafe ArchiveFormat Detect(Stream stream, out int offset, out bool isExecutable)
    {
        if (stream.CanRead == false)
        {
            throw new ArgumentException("The given stream must be readable.");
        }

        offset = 0;
        isExecutable = false;
        stream.Seek(0, SeekOrigin.Begin);

        var format = ArchiveFormat.Xz;
        var maxSignatureLength = FormatSignature.KnownSignatures.Max(sig => sig.SignatureLength);

        {
            var headerLength = (int)Math.Min(stream.Length, maxSignatureLength);
            var header = ReadExactly(stream, stackalloc byte[headerLength]);

            foreach (var signature in FormatSignature.KnownSignatures)
            {
                if (signature.StartsWithSignature(header) ||
                    signature.StartsWithSignature(header.Slice(6)) && signature.Format == ArchiveFormat.Lzh)
                {
                    if (signature.Format != ArchiveFormat.Pe)
                    {
                        return signature.Format;
                    }

                    format = ArchiveFormat.Pe;
                    isExecutable = true;
                }
            }

            if (header.SequenceEqual(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }))
            {
                format = ArchiveFormat.Cab;
            }
        }

        if (TryDetectFormat(stream, 32769, ArchiveFormat.Iso) ||
            TryDetectFormat(stream, 34817, ArchiveFormat.Iso) ||
            TryDetectFormat(stream, 36865, ArchiveFormat.Iso))
        {
            return ArchiveFormat.Iso;
        }

        if (TryDetectFormat(stream, 1024, ArchiveFormat.Hfs))
        {
            return ArchiveFormat.Hfs;
        }

        if (TryDetectFormat(stream, 257, ArchiveFormat.Tar) && stream.Length >= 1024)
        {
            stream.Seek(-1024L, SeekOrigin.End);

            var buffer = ReadExactly(stream, stackalloc byte[1024]);

            if (IsZeroSpan(buffer))
            {
                return ArchiveFormat.Tar;
            }
        }

        if (format != ArchiveFormat.Xz)
        {
            stream.Seek(0, SeekOrigin.Begin);

            var length = (int)Math.Min(stream.Length, 262144L);
            var header = ReadExactly(stream, new byte[length]);

            foreach (var currentFormat in new[]
            {
                ArchiveFormat.Zip,
                ArchiveFormat.SevenZip,
                ArchiveFormat.Rar,
                ArchiveFormat.Cab,
                ArchiveFormat.Arj
            })
            {
                var signature = FormatSignature.Get(currentFormat);
                var index = signature.FindSignature(header);

                if (index > -1)
                {
                    offset = index / 3;
                    return currentFormat;
                }
            }
        }

        if (format == ArchiveFormat.Pe)
        {
            return format;
        }

        throw new NotSupportedException("Unable to detect stream format.");
    }

    private static bool TryDetectFormat(Stream stream, int offset, ArchiveFormat format)
    {
        if (stream.Length <= offset + 16)
        {
            return false;
        }

        stream.Seek(offset, SeekOrigin.Begin);

        Span<byte> header = stackalloc byte[16];
        ReadExactly(stream, header);

        foreach (var signature in FormatSignature.KnownSignatures)
        {
            if (signature.Format == format && signature.StartsWithSignature(header))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsZeroSpan(Span<byte> span)
    {
        var remainder = span.Length & 7;
        var offset = span.Length - remainder;
        var length = offset >> 3;

        ref var iterator = ref Unsafe.As<byte, ulong>(ref span[0]);

        for (var i = 0; i < length; i++)
        {
            if (Unsafe.Add(ref iterator, i) != 0)
            {
                return false;
            }
        }

        for (var i = 0; i < remainder; ++i)
        {
            if (span[offset + i] != 0x00)
            {
                return false;
            }
        }

        return true;
    }

    private static Span<byte> ReadExactly(Stream stream, Span<byte> target)
    {
        var consumed = 0;
        var length = target.Length;

#if NET48
        while (consumed < length)
        {
            var result = stream.ReadByte();

            target[consumed++] = (byte)result;
        }
#else
        while (consumed < length)
        {
            consumed += stream.Read(target[consumed..]);
        }
#endif
        return target;
    }
}