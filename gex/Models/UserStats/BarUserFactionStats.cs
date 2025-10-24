using Dapper.ColumnMapper;
using gex.Code;
using gex.Common.Code.Constants;
using System;

namespace gex.Models.UserStats {

    [DapperColumnsMapped]
    public class BarUserFactionStats {

        [ColumnMapping("user_id")]
        public long UserID { get; set; }

        [ColumnMapping("faction")]
        public byte Faction { get; set; } = BarFaction.DEFAULT;

        [ColumnMapping("gamemode")]
        public byte Gamemode { get; set; } = BarGamemode.DEFAULT;

        [ColumnMapping("play_count")]
        public int PlayCount { get; set; }

        [ColumnMapping("win_count")]
        public int WinCount { get; set; }

        [ColumnMapping("loss_count")]
        public int LossCount { get; set; }

        [ColumnMapping("tie_count")]
        public int TieCount { get; set; }

        [ColumnMapping("last_updated")]
        public DateTime LastUpdated { get; set; }

    }
}
