using System;
using System.Text.Json.Serialization;

namespace gex.Models.Bar.Commands {

    public class BarCommandInsert : BarCommand {

        [JsonConstructor]
        private BarCommandInsert() { }

        public BarCommandInsert(Span<float> parameters) {
            QueuePosition = (int)parameters[0];
        }

        public int QueuePosition { get; set; }

        public BarCommand? Command { get; set; } = null;

    }
}
