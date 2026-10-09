using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Common.Code {

    /*
        The MIT License (MIT)

        Copyright (c) 2021 Jazcash

        Permission is hereby granted, free of charge, to any person obtaining a copy of
        this software and associated documentation files (the "Software"), to deal in
        the Software without restriction, including without limitation the rights to
        use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
        the Software, and to permit persons to whom the Software is furnished to do so,
        subject to the following conditions:

        The above copyright notice and this permission notice shall be included in all
        copies or substantial portions of the Software.

        THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
        FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
        COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
        IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
        CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
     */

    /// <summary>
    ///     helper class to decompress DXT1 data.
    ///     this code was ported from Beyond All Reason:
    ///     https://github.com/beyond-all-reason/map-parser
    ///     and is licensed under MIT
    /// </summary>
    public class Dxt1 {

        private const int DXT1BlockSize = 8;
        private const int RGBABlockSize = 64;
        private const int BlockWidth = 4;
        private const int BlockHeight = 4;

        /// <summary>
        ///     decompress a byte span that contains a DXT1 image
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static byte[] Decompress(int width, int height, Span<byte> data) {
            if (width % BlockWidth != 0) {
                throw new Exception("Width of the texture must be divisible by 4");
            }
            if (height % BlockHeight != 0) {
                throw new Exception("Height of the texture must be divisible by 4");
            }
            if (width < BlockWidth || height < BlockHeight) {
                throw new Exception("Size of the texture is to small");
            }

            int w = width / BlockWidth;
            int h = height / BlockHeight;
            int blockNumber = w * h;
            //if (blockNumber * DXT1BlockSize != data.length) throw new Error("Data does not match dimensions");
            byte[] output = new byte[(width * height * 4)];
            for (int i = 0; i < blockNumber; i++) {
                byte[] decompressed = DecompressBlockDXT1(data.Slice(i * DXT1BlockSize, DXT1BlockSize));

                //using StreamWriter append = File.AppendText("out-decompress-blocks-gex.bin");
                //append.WriteLine(string.Join(' ', decompressed.ToArray()));
                //append.Close();

                int pixelX = (i % w) * 4;
                int pixelY = i / w * 4;

                int j = 0;
                for (int y = 0; y < 4; y++) {
                    for (int x = 0; x < 4; x++) {
                        int px = x + pixelX;
                        int py = y + pixelY;
                        int outIndex = (py * width + px) * 4;
                        output[outIndex] = decompressed[j];
                        output[outIndex + 1] = decompressed[j + 1];
                        output[outIndex + 2] = decompressed[j + 2];
                        output[outIndex + 3] = decompressed[j + 3];
                        j += 4;
                    }
                }
            }

            return output;
        }

        private static byte[] DecompressBlockDXT1(Span<byte> data) {
            ushort cVal0 = (ushort)((data[1] << 8) + data[0]);
            ushort cVal1 = (ushort)((data[3] << 8) + data[2]);
            byte[] lookup = GenerateDXT1Lookup(cVal0, cVal1);
            byte[] output = new byte[RGBABlockSize];

            for (int i = 0; i < 16; i++) {
                int bitOffset = i * 2;
                int b = 4 + bitOffset / 8;
                byte bits = (byte)((data[b] >> bitOffset % 8) & 3);
                output[i * 4 + 0] = lookup[bits * 4 + 0];
                output[i * 4 + 1] = lookup[bits * 4 + 1];
                output[i * 4 + 2] = lookup[bits * 4 + 2];
                output[i * 4 + 3] = lookup[bits * 4 + 3];
            }
            return output;
        }

        private static byte[] GenerateDXT1Lookup(ushort colorValue0, ushort colorValue1) {
            SKColor color0 = GetComponentsFromRGB565(colorValue0);
            SKColor color1 = GetComponentsFromRGB565(colorValue1);
            byte[] lookup = new byte[16];
            if (colorValue0 > colorValue1) {
                // Non transparent mode
                lookup[0] = color0.Red;
                lookup[1] = color0.Green;
                lookup[2] = color0.Blue;
                lookup[3] = 255;
                lookup[4] = color1.Red;
                lookup[5] = color1.Green;
                lookup[6] = color1.Blue;
                lookup[7] = 255;
                lookup[8] = (byte)((color0.Red * 2 + color1.Red) / 3);
                lookup[9] = (byte)((color0.Green * 2 + color1.Green) / 3);
                lookup[10] = (byte)((color0.Blue * 2 + color1.Blue) / 3);
                lookup[11] = 255;
                lookup[12] = (byte)((color0.Red + color1.Red * 2) / 3);
                lookup[13] = (byte)((color0.Green + color1.Green * 2) / 3);
                lookup[14] = (byte)((color0.Blue + color1.Blue * 2) / 3);
                lookup[15] = 255;
            } else {
                // transparent mode
                lookup[0] = color0.Red;
                lookup[1] = color0.Green;
                lookup[2] = color0.Blue;
                lookup[3] = 255;
                lookup[4] = color1.Red;
                lookup[5] = color1.Green;
                lookup[6] = color1.Blue;
                lookup[7] = 255;
                lookup[8] = (byte)((color0.Red + color1.Red) / 2);
                lookup[9] = (byte)((color0.Green + color1.Green) / 2);
                lookup[10] = (byte)((color0.Blue + color1.Blue) / 2);
                lookup[11] = 255;
                lookup[12] = 0;
                lookup[13] = 0;
                lookup[14] = 0;
                lookup[15] = 0;
            }
            return lookup;
        }

        private static SKColor GetComponentsFromRGB565(ushort color) {
            return new SKColor(
                red:    (byte)((color & 0b11111000_00000000) >> 8),
                green:  (byte)((color & 0b00000111_11100000) >> 3),
                blue:   (byte)((color & 0b00000000_00011111) << 3)
            );
        }

    }
}
