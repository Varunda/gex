using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace gex.Models.Bar.Commands.Types {

    public class BarCommandTypeIconMap : BarCommand {

        [JsonConstructor]
        private BarCommandTypeIconMap() { }

        public BarCommandTypeIconMap(Span<float> parameters) {
            Debug.Assert(parameters.Length >= 3, $"expected >=3 parameters, got {parameters.Length} instead");
            X = parameters[0];
            Y = parameters[1];
            Z = parameters[2];
        }

        public float X { get; set; }

        public float Y { get; set; }

        public float Z { get; set; }

    }
}
