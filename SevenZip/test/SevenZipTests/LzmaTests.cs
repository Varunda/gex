using System.Linq;
using SevenZip;
using Xunit;

namespace SevenZipTests;

public class LzmaTests
{
    const string Data = "This is a sample text used to test the LZMA algorithm.";

    [Fact]
    public void CompressAndDecompressArray()
    {
        var compressed = Lzma.Compress(EncodeString(Data));
        var decompressed = Lzma.Decompress(compressed);
        var result = DecodeString(decompressed);

        Assert.Equal(Data, result);
    }

    [Theory]
    [InlineData(
        "5D,00,00,40,00,36,00,00,00,00,00,00,00,00,2A,1A,09,27,64,1C,87,8A,4F,CA,43,56,59,3F,44,E5,90,A1,26,59,E4,FC,3B,9A,E3,24,8D,0B,BA,C4,9E,9A,E0,7D,D6,15,46,88,AD,33,82,56,4B,E9,3B,E1,A2,99,66,28,0E,1A,F6,00",
        Data)]
    public void Decompress(string data, string expected)
    {
        var compressed = HexStringToByteArray(data);
        var decompressed = Lzma.Decompress(compressed);

        var result = DecodeString(decompressed);

        Assert.Equal(expected, result);
    }

    private static byte[] HexStringToByteArray(string data) =>
        data
            .Split(',')
            .Select(b => byte.Parse(b, System.Globalization.NumberStyles.HexNumber))
            .ToArray();

    private static byte[] EncodeString(string value) => System.Text.Encoding.UTF8.GetBytes(value);

    private static string DecodeString(byte[] value) => System.Text.Encoding.UTF8.GetString(value);
}