using System.Collections;
using System.Collections.Generic;

namespace gex.Code.Constants {

    public class BarGamemode {

        public const byte DEFAULT = 0;

        public const byte DUEL = 1;

        public const byte SMALL_TEAM = 2;

        public const byte LARGE_TEAM = 3;

        public const byte FFA = 4;

        public const byte TEAM_FFA = 5;

        public static string GetName(byte gamemode) {
            return gamemode switch {
                DEFAULT => "default",
                DUEL => "Duel",
                SMALL_TEAM => "Small team",
                LARGE_TEAM => "Large team",
                FFA => "FFA",
                TEAM_FFA => "Team FFA",
                _ => $"unknown {gamemode}"
            };
        }

        public static byte GetID(string name) {
            return name.ToLower() switch {
                "duel" => DUEL,
                "small team" => SMALL_TEAM,
                "large team" => LARGE_TEAM,
                "ffa" => FFA,
                "team ffa" => TEAM_FFA,
                _ => DEFAULT
            };
        }

        public static readonly IEnumerable<byte> List = [
            DUEL, SMALL_TEAM, LARGE_TEAM, FFA, TEAM_FFA
        ];

    }
}
