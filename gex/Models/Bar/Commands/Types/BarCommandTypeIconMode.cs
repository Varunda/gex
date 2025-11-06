using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace gex.Models.Bar.Commands.Types {

    public class BarCommandTypeIconMode : BarCommand {

        [JsonConstructor]
        private BarCommandTypeIconMode() { }

        public BarCommandTypeIconMode(Span<float> parameters) {
            Debug.Assert(parameters.Length >= 1, $"expected >=1 parameter, got {parameters.Length} instead");
            Mode = parameters[0];
        }

        public float Mode { get; set; }

    }
}
