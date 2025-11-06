using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace gex.Models.Bar.Commands {

    public class BarCommandCustom : BarCommand {

        [JsonConstructor]
        private BarCommandCustom() { }

        public BarCommandCustom(Span<float> parameters) {
            Parameters = parameters.ToArray().ToList();
        }

        public List<float> Parameters { get; set; } = [];

    }
}
