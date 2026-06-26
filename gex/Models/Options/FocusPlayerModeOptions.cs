using System.Collections.Generic;

namespace gex.Models.Options {

    public class FocusPlayerModeOptions {

        public bool Enabled { get; set; }

        public List<long> UserIDs { get; set; } = [];

    }
}
