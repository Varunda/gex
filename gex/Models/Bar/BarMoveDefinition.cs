
namespace gex.Models.Bar {

    public class BarMoveDefinition {

        public string Name { get; set; } = "";

        public double FootprintX { get; set; }

        public double FootprintZ { get; set; }

        public double CrushStrength { get; set; }

        public double DepthMod { get; set; }

        public double MaxSlope { get; set; }

        public double MinWaterDepth { get; set; }

        public double MaxWaterDepth { get; set; }

        public double MaxWaterSlope { get; set; }

        public double SlopeMod { get; set; }

        public double SpeedModClass { get; set; }

        public bool Submarine { get; set; }

        public bool OverrideUnitWaterline { get; set; }

    }
}
