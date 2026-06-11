namespace gex.Models.Bar {

    // https://springrts.com/wiki/Mapdev:SMF_format
    public class BarMapFileHeader {

        public int ID { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public int SquareSize { get; set; }

        public int TexelsPerSquare { get; set; }

        public int TileSize { get; set; }

        public float MinHeight { get; set; }

        public float MaxHeight { get; set; }

        public int HeightMapOffset { get; set; }

        public int TypeMapOffset { get; set; }

        public int TileIndexOffset { get; set; }

        public int MiniMapOffset { get; set; }

        public int MetalMapOffset { get; set; }

        public int FeatureMapOffset { get; set; }

        public ushort[] HeightMap { get; set; } = [];

    }
}
