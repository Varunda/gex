using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Common.Models.Map {

    public class BarMapSmt {

        public int Version { get; set; }

        public int TileCount { get; set; }

        public int TileSize { get; set; } = 32;

        public int CompressionType { get; set; } = 1; // 1 = DXT1

        public SKBitmap Bitmap { get; set; } = new();

    }
}
