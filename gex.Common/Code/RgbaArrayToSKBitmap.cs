using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Common.Code {

    public static class RgbaArrayToSKBitmap {

        public static SKColor[] Convert(byte[] rgba, SKColor[] pixels) {
            if (rgba.Length % 4 != 0) {
                throw new Exception($"rgba must have a multiple of 4 elements");
            }

            if (pixels.Length * 4 != rgba.Length) {
                throw new Exception($"the length of the input pixels ({pixels.Length}) must be 4 times the input rgba ({rgba.Length})");
            }

            Dictionary<int, SKColor> colorCache = [];

            for (int i = 0; i < rgba.Length; i += 4) {
                int cacheKey = (rgba[i + 0] << 24)
                    | (rgba[i + 1] << 16)
                    | (rgba[i + 2] << 8)
                    | (rgba[i + 3] << 0);

                if (colorCache.TryGetValue(cacheKey, out SKColor value) == false) {
                    value = new SKColor(
                        red: rgba[i + 0],
                        green: rgba[i + 1],
                        blue: rgba[i + 2],
                        alpha: rgba[i + 3]
                    );
                    colorCache[cacheKey] = value;
                }

                pixels[i / 4] = value;
            }

            return pixels;

        }

    }
}
