using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace gex.Models.Bar.Commands.Types {

    public class BarCommandTypeIconUnitOrMap : BarCommand {

        [JsonConstructor]
        private BarCommandTypeIconUnitOrMap() { }

        public BarCommandTypeIconUnitOrMap(Span<float> parameters) {
            Debug.Assert(parameters.Length == 1 || parameters.Length >= 3, $"expected 1 or >=3 parameters, got {parameters.Length} instead");
            if (parameters.Length == 1) {
                UnitID = (int)parameters[0];
            } else {
                X = parameters[0];
                Y = parameters[1];
                Z = parameters[2];
            }
        }

        public int UnitID { get; set; }

        public float X { get; set; }

        public float Y { get; set; }

        public float Z { get; set; }

    }
}
