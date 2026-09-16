using System.IO;
using SevenZip.CoreSdk;
using SevenZip.CoreSdk.Compression.LZMA;

namespace SevenZip;

/// <summary>
/// The <see cref="Lzma"/> class provides a set of static methods to easily compress or decompress
/// binary data using the LZMA compression method.
/// 
/// The methods this class offers internally use the code and algorithms from the original
/// 7z LZMA SDK. See https://7-zip.org/sdk.html for details.
/// </summary>
public static class Lzma
{
    /// <summary>
    /// The progress delegate can be used by the <c>Compress</c> and <c>Decompress</c> methods to
    /// receive notifications about the current progress. The progress may, however, be -1 in case
    /// the compressor or decompressor cannot determine the sizes in advance.
    /// </summary>
    /// <param name="inputSize">The number of bytes that were read.</param>
    /// <param name="outputSize">The number of bytes already written.</param>
    public delegate void Progress(long inputSize, long outputSize);

    /// <summary>
    /// Compresses the content of the <paramref name="source"/> stream and writes the compressed
    /// data to the <paramref name="target"/> stream.
    /// </summary>
    /// <param name="source">The stream containing the data to compress.</param>
    /// <param name="target">The stream to write the compressed data to.</param>
    /// <param name="progress">An optional instance of the <see cref="Progress"/> delegate
    /// which can be used to receive progress updates during compression.</param>
    /// <returns>The size of the compressed stream.</returns>
    public static long Compress(Stream source, Stream target, Progress progress = null)
    {
        var position = target.Position;
        var encoder = CreateEncoderWithDefaultProperties();

        WriteEncoderProperties(encoder, target);
        WriteLength64(source.Length, target);

        encoder.Code(source, target, -1L, -1L, new ProgressProxy(progress));
        return target.Position - position;
    }

    /// <summary>
    /// Compresses the given <paramref name="content"/> and returns a new array containing
    /// the compressed data.
    /// </summary>
    /// <param name="content">The buffer containing the data to compress.</param>
    /// <param name="progress">An optional instance of the <see cref="Progress"/> delegate
    /// which can be used to receive progress updates during compression.</param>
    /// <returns>An array containing the compressed data.</returns>
    public static byte[] Compress(byte[] content, Progress progress = null)
    {
        using var source = new MemoryStream(content);
        using var target = new MemoryStream();

        Compress(source, target, progress);
        return target.ToArray();
    }

    /// <summary>
    /// Decompresses the data from the given <paramref name="source"/> stream and writes it to the
    /// <paramref name="target"/> stream.
    /// </summary>
    /// <param name="source">The stream providing the compressed data.</param>
    /// <param name="target">The stream to write the decompressed data to.</param>
    /// <param name="progress">An optional instance of the <see cref="Progress"/> delegate
    /// which can be used to receive progress updates during compression.</param>
    public static void Decompress(Stream source, Stream target, Progress progress = null)
    {
        var decoder = new Decoder();
        var properties = ReadDecoderProperties(source);
        var length = ReadLength64(source);

        decoder.SetDecoderProperties(properties);
        decoder.Code(source, target, source.Length - source.Position, length, new ProgressProxy(progress));
    }

    /// <summary>
    /// Decompresses the contents of the <paramref name="compressed"/> array and returns a new array
    /// containing the decompressed data.
    /// </summary>
    /// <param name="compressed">The array containing the compressed data.</param>
    /// <param name="progress">An optional instance of the <see cref="Progress"/> delegate
    /// which can be used to receive progress updates during compression.</param>
    /// <returns>An array containing the decompressed data.</returns>
    public static byte[] Decompress(byte[] compressed, Progress progress = null)
    {
        using var source = new MemoryStream(compressed);
        using var target = new MemoryStream();

        Decompress(source, target, progress);
        return target.ToArray();
    }

    private const int DictionarySize = 4194304;

    private static byte[] ReadDecoderProperties(Stream source)
    {
        var buffer = new byte[5];

        ReadExactly(source, buffer, 0, buffer.Length);

        return buffer;
    }

    private static long ReadLength64(Stream source)
    {
        var length = 0L;

        for (var i = 0; i < 8; ++i)
        {
            var value = source.ReadByte();

            if (value < 0)
            {
                throw new EndOfStreamException("The source stream did not contain the required number of bytes.");
            }

            length |= ((long)((byte)value) << (i * 8));
        }

        return length;
    }

    private static void ReadExactly(Stream stream, byte[] buffer, int offset, int length)
    {
        var cursor = 0;

        while (cursor < length)
        {
            var result = stream.Read(buffer, offset, length - cursor);

            if (result == 0)
            {
                throw new EndOfStreamException("The source stream did not contain the required number of bytes.");
            }

            cursor += result;
            offset += result;
        }
    }

    private static Encoder CreateEncoderWithDefaultProperties()
    {
        var encoder = new Encoder();
        var keys = new CoderPropID[]
        {
            CoderPropID.DictionarySize,
            CoderPropID.PosStateBits,
            CoderPropID.LitContextBits,
            CoderPropID.LitPosBits,
            CoderPropID.Algorithm,
            CoderPropID.NumFastBytes,
            CoderPropID.MatchFinder,
            CoderPropID.EndMarker,
        };

        var values = new object[]
        {
            DictionarySize,
            2,
            3,
            0,
            2,
            256,
            "bt4",
            false
        };

        encoder.SetCoderProperties(keys, values);
        return encoder;
    }

    private static void WriteEncoderProperties(IWriteCoderProperties encoder, Stream target)
    {
        encoder.WriteCoderProperties(target);
    }

    private static void WriteLength64(long length, Stream target)
    {
        for (var i = 0; i < 8; ++i)
        {
            target.WriteByte((byte)(length >> (8 * i)));
        }
    }

    #region ProgressProxy
    private sealed class ProgressProxy : ICodeProgress
    {
        private readonly Progress _callback;

        public ProgressProxy(Progress callback) => _callback = callback;

        void ICodeProgress.SetProgress(long inSize, long outSize) => _callback?.Invoke(inSize, outSize);
    }
    #endregion
}
