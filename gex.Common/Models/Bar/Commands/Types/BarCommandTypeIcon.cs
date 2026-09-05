using gex.Common.Models.Bar;
using System;
using System.Text.Json.Serialization;

namespace gex.Common.Models.Bar.Commands.Types {

    public class BarCommandTypeIcon : BarCommand {

        [JsonConstructor]
        private BarCommandTypeIcon() { }

        public BarCommandTypeIcon(Span<float> parms) {

        }

    }
}
