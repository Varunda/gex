using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace gex.Models.Bar.Commands.Types {

    public class BarCommandTypeIconUnit : BarCommand {

        [JsonConstructor]
        private BarCommandTypeIconUnit() { }

        public BarCommandTypeIconUnit(Span<float> parameters) {
            Debug.Assert(parameters.Length >= 1, $"expected >=1 parameter, got {parameters.Length} instead");
            UnitID = (int)parameters[0];
        }

        public int UnitID { get; set; }

    }
}
