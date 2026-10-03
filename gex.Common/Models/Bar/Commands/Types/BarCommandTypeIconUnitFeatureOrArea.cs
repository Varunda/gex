using gex.Common.Models.Bar;
using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace gex.Common.Models.Bar.Commands.Types {

    public class BarCommandTypeIconUnitFeatureOrArea : BarCommand {

        [JsonConstructor]
        private BarCommandTypeIconUnitFeatureOrArea() { }

        public BarCommandTypeIconUnitFeatureOrArea(Span<float> parameters) {
            Debug.Assert(parameters.Length == 1 || parameters.Length == 3 || parameters.Length == 4 || parameters.Length == 5,
                $"expected 1, 3, 4 or 5 parameters, got {parameters.Length} instead");

            if (parameters.Length == 1) {
                UnitID = (int)parameters[0];
            } else if (parameters.Length == 3) {
                X = parameters[0];
                Y = parameters[1];
                Z = parameters[2];
            } else if (parameters.Length == 4) {
                X = parameters[0];
                Y = parameters[1];
                Z = parameters[2];
                Radius = parameters[3];
            } else if (parameters.Length == 5) {
                UnitID = (int)parameters[0];
                X = parameters[1];
                Y = parameters[2];
                Z = parameters[3];
                Radius = parameters[4];
            }
        }

        public int UnitID { get; set; }

        public float X { get; set; }

        public float Y { get; set; }

        public float Z { get; set; }

        public float Radius { get; set; }
    }
}
