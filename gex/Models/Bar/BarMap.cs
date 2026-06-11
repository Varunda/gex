using Dapper.ColumnMapper;
using gex.Code;
using gex.Common.Code.Constants;
using System;
using System.Text.Json;

namespace gex.Models.Bar {

    [DapperColumnsMapped]
    public class BarMap {

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

        [ColumnMapping("start_position_data")]
        public JsonElement? StartPositionData { get; set; }

        [ColumnMapping("symmetry_axis")]
        public MapSymmetryAxis? SymmetryAxis { get; set; } = null;

        [ColumnMapping("timestamp")]
        public DateTime Timestamp { get; set; }

    }
}
