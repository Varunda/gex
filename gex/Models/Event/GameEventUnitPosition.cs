using Dapper.ColumnMapper;
using gex.Code;
using System;

namespace gex.Models.Event {

    [DapperColumnsMapped]
    public class GameEventUnitPosition : GameEvent {

        [JsonActionLogPropertyName("unitID")]
        [ColumnMapping("unit_id")]
        public int UnitID { get; set; }

        [JsonActionLogPropertyName("teamID")]
        [ColumnMapping("team_id")]
        public int TeamID { get; set; }

        [JsonActionLogPropertyName("x")]
        [ColumnMapping("x")]
        public double X { get; set; }

        [JsonActionLogPropertyName("y")]
        [ColumnMapping("y")]
        public double Y { get; set; }

        [JsonActionLogPropertyName("z")]
        [ColumnMapping("z")]
        public double Z { get; set; }

        public override bool Equals(object? obj) {
            return obj is GameEventUnitPosition position &&
                   GameID == position.GameID &&
                   Frame == position.Frame &&
                   UnitID == position.UnitID &&
                   TeamID == position.TeamID &&
                   X == position.X &&
                   Y == position.Y &&
                   Z == position.Z;
        }

        public override int GetHashCode() {
            return HashCode.Combine(GameID, Frame, UnitID, TeamID, X, Y, Z);
        }
    }
}
