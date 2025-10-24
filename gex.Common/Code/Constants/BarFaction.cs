using System.Collections.Generic;

namespace gex.Common.Code.Constants {

    public class BarFaction {

        public const byte DEFAULT = 0;

        public const byte ARMADA = 1;

        public const byte CORTEX = 2;

        public const byte LEGION = 3;

        public const byte RANDOM = 4;

        public static readonly IEnumerable<byte> List = [
            ARMADA, CORTEX, LEGION, RANDOM
        ];

        public static string GetName(byte id) {
            if (id == 0) {
                return "Default";
            } else if (id == ARMADA) {
                return "Armada";
            } else if (id == CORTEX) {
                return "Cortex";
            } else if (id == LEGION) {
                return "Legion";
            } else if (id == RANDOM) {
                return "Random";
            }
            return $"<unchecked {id}>";
        }

        public static byte GetId(string name) {
            if (name == "Armada") {
                return ARMADA;
            } else if (name == "Cortex") {
                return CORTEX;
            } else if (name == "Legion") {
                return LEGION;
            } else if (name == "Random") {
                return RANDOM;
            }
            return DEFAULT;
        }

    }
}
