using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace gex.Models.Bar.Commands.Types {

    public class BarCommandTypeIconUnitOrArea : BarCommand {

        [JsonConstructor]
        private BarCommandTypeIconUnitOrArea() { }

        public BarCommandTypeIconUnitOrArea(Span<float> parameters) {
            Debug.Assert(parameters.Length == 1 || parameters.Length >= 4, $"expected 1 or >=4 parameters, got {parameters.Length} instead");
            if (parameters.Length == 1) {
                UnitID = (int)parameters[0];
            } else {
                X = parameters[0];
                Y = parameters[1];
                Z = parameters[2];
                Radius = parameters[3];
            }
        }

        public int UnitID { get; set; }

        public float X { get; set; }

        public float Y { get; set; }

        public float Z { get; set; }

        public float Radius { get; set; }

    }
}
