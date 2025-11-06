using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace gex.Models.Bar.Commands.Types {

    public class BarCommandTypeIconArea : BarCommand {

        [JsonConstructor]
        private BarCommandTypeIconArea() { }

        public BarCommandTypeIconArea(Span<float> parameters) {
            Debug.Assert(parameters.Length >= 4, $"expected >=4 parameters, got {parameters.Length} instead");
            X = parameters[0];
            Y = parameters[1];
            Z = parameters[2];
            Radius = parameters[3];
        }

        public float X { get; set; }

        public float Y { get; set; }

        public float Z { get; set; }

        public float Radius { get; set; }

    }
}
