using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace gex.Models.Bar.Commands.Types {

    public class BarCommandTypeNumber : BarCommand {

        [JsonConstructor]
        private BarCommandTypeNumber() { }

        public BarCommandTypeNumber(Span<float> parameters) {
            Debug.Assert(parameters.Length >= 1, $"expected >=1 parameter, got {parameters.Length} instead");
            Value = parameters[0];
        }

        public float Value { get; set; }

    }
}
