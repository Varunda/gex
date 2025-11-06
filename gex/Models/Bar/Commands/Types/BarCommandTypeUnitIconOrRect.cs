using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace gex.Models.Bar.Commands.Types {

    public class BarCommandTypeUnitIconOrRect : BarCommand {

        [JsonConstructor]
        private BarCommandTypeUnitIconOrRect() { }

        public BarCommandTypeUnitIconOrRect(Span<float> parameters) {
            Debug.Assert(parameters.Length == 1 || parameters.Length == 6, $"expected 1 or 6 parameters, got {parameters.Length} instead");
            if (parameters.Length == 1) {
                UnitID = (int)parameters[0];
            } else {
                StartX = parameters[0];
                StartY = parameters[1];
                StartZ = parameters[2];
                EndX = parameters[3];
                EndY = parameters[4];
                EndZ = parameters[5];
            }
        }

        public int UnitID { get; set; }

        public float StartX { get; set; }

        public float StartY { get; set; }

        public float StartZ { get; set; }

        public float EndX { get; set; }

        public float EndY { get; set; }

        public float EndZ { get; set; }

    }
}
