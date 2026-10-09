using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Common.Models.Map {

    public class BarMapData {

        public int ID { get; set; }

        public string Name { get; set; } = "";

        public string FileName { get; set; } = "";

        public string Description { get; set; } = "";

        public double TidalStrength { get; set; }

        public double MaxMetal { get; set; }

        public double ExtractorRadius { get; set; }

        public double MinimumWind { get; set; }

        public double MaximumWind { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        public string Author { get; set; } = "";

        public string NormalMapFilename { get; set; } = "";

        public string SpecularMapFilename { get; set; } = "";

        public BarMapFileHeader Header { get; set; } = new();

        public BarMapSmt? Smt { get; set; } = null;

        public SKBitmap? NormalMap { get; set; } = null;

        public SKBitmap? SpeculaMap { get; set; } = null;
        
    }
}
