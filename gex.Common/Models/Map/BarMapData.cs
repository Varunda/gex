using gex.Common.Models.Bar;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Common.Models.Map {

    public class BarMapData {

        public BarMapData() { }

        public BarMapData(BarMapData data) {
            ID = data.ID;
            Name = data.Name;
            FileName = data.FileName;
            Description = data.Description;
            TidalStrength = data.TidalStrength;
            MaxMetal = data.MaxMetal;
            ExtractorRadius = data.ExtractorRadius;
            MinimumWind = data.MinimumWind;
            MaximumWind = data.MaximumWind;
            Width = data.Width;
            Height = data.Height;
            Author = data.Author;
            NormalMapFilename = data.NormalMapFilename;
            SpecularMapFilename = data.SpecularMapFilename;
            Header = data.Header;
            Smt = data.Smt;
            NormalMap = data.NormalMap;
            SpeculaMap = data.SpeculaMap;
            MetalSpots = data.MetalSpots == null ? null : new List<BarMapMetalSpot>(data.MetalSpots);
            Features = [.. data.Features];
            FeatureSet = data.FeatureSet;
            ReclaimableEnergy = data.ReclaimableEnergy;
            ReclaimableMetal = data.ReclaimableMetal;
            GeothermalCount = data.GeothermalCount;
        }

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

        public List<BarMapMetalSpot>? MetalSpots { get; set; } = null;

        public List<BarFeature> Features { get; set; } = [];

        public BarFeatureSet? FeatureSet { get; set; } = null;

        /// <summary>
        ///     total energy reclaimable in the map, null if not parsed (the feature set is null)
        /// </summary>
        public double? ReclaimableEnergy { get; set; } = null;

        /// <summary>
        ///     total metal reclaimable in the map, null if not parsed (the feature set is null)
        /// </summary>
        public double? ReclaimableMetal { get; set; } = null;

        /// <summary>
        ///     how many geothermal vents are in the map, or null if not parsed (the feature set is null)
        /// </summary>
        public int? GeothermalCount { get; set; } = null;
        
    }
}
