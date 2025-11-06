using gex.Common.Code.Constants;
using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace gex.Models.Bar.Commands {

    public class BarCommandBuild : BarCommand {

        [JsonConstructor]
        private BarCommandBuild() { }

        public BarCommandBuild(Span<float> parms) {
            Debug.Assert(parms.Length == 0 || parms.Length >= 3 || (parms.Length == 1 && parms[0] == 0), $"expected parameters to be 0 or >= 3, was {parms.Length} instead");
            if (parms.Length == 0 || (parms.Length == 1 && parms[0] == 0)) {
                IsFromFactory = true;
            } else {
                X = parms[0];
                Y = parms[1];
                Z = parms[2];
                Facing = parms.Length >= 4 ? parms[3] : 0;
            }
        }

        public int UnitDefinitionID { get; set; }

        public float X { get; set; }

        public float Y { get; set; }

        public float Z { get; set; }

        public float Facing { get; set; }

        public bool IsFromFactory { get; set; } = false;

    }
}
