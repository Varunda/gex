using Dapper.ColumnMapper;
using gex.Common.Code;
using gex.Common.Code.Constants;

namespace gex.Common.Models.Map {

    [DapperColumnsMapped]
    public class BarMap {

        public BarMap() { }

        public BarMap(BarMapData data) {
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
        }

        [ColumnMapping("id")]
        public int ID { get; set; }

        [ColumnMapping("name")]
        public string Name { get; set; } = "";

        [ColumnMapping("filename")]
        public string FileName { get; set; } = "";

        [ColumnMapping("description")]
        public string Description { get; set; } = "";

        [ColumnMapping("tidal_strength")]
        public double TidalStrength { get; set; }

        [ColumnMapping("max_metal")]
        public double MaxMetal { get; set; }

        [ColumnMapping("extractor_radius")]
        public double ExtractorRadius { get; set; }

        [ColumnMapping("minimum_wind")]
        public double MinimumWind { get; set; }

        [ColumnMapping("maximum_wind")]
        public double MaximumWind { get; set; }

        [ColumnMapping("width")]
        public double Width { get; set; }

        [ColumnMapping("height")]
        public double Height { get; set; }

        [ColumnMapping("author")]
        public string Author { get; set; } = "";

        public StartSpotData? StartPositionData { get; set; } = null;

        [ColumnMapping("symmetry_axis")]
        public MapSymmetryAxis? SymmetryAxis { get; set; } = null;

        [ColumnMapping("timestamp")]
        public DateTime Timestamp { get; set; }

    }
}
