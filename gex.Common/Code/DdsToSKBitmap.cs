using Pfim;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace gex.Common.Code {

    public static class DdsToSKBitmap {

        public static SKBitmap Convert(IImage dds) {
            SKColorType colorType = SKColorType.Bgra8888;

            if (dds.Format != ImageFormat.Rgba32) {
                throw new NotImplementedException($"only rgba32 dds images are supported");
            }

            SKImageInfo imageInfo = new(dds.Width, dds.Height, colorType);
            GCHandle handle = GCHandle.Alloc(dds.Data, GCHandleType.Pinned);
            nint ptr = Marshal.UnsafeAddrOfPinnedArrayElement(dds.Data, 0);
            using SKData skData = SKData.Create(ptr, dds.DataLen, (addr, ctx) => handle.Free());
            using SKImage skImage = SKImage.FromPixels(imageInfo, skData, dds.Stride);
            SKBitmap bitmap = SKBitmap.FromImage(skImage);

            return bitmap;
        }

    }
}
