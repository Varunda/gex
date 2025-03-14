using System.Security.Permissions;

namespace gex.Models.Bar {

    public class BarMap {

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

    }
}
