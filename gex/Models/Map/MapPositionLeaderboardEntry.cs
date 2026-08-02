using Dapper.ColumnMapper;
using gex.Code;
using gex.Models.Bar;
using gex.Models.UserStats;
using System;

namespace gex.Models.Map {

    [DapperColumnsMapped]
    public class MapPositionLeaderboardEntry {

        /// <summary>
        ///     ID of the <see cref="BarUser"/>
        /// </summary>
        [ColumnMapping("user_id")]
        public long UserID { get; set; }

        /// <summary>
        ///     internal name of the <see cref="BarMap"/>
        /// </summary>
        [ColumnMapping("map_filename")]
        public string MapFilename { get; set; } = "";

        /// <summary>
        ///     role label
        /// </summary>
        [ColumnMapping("position_label")]
        public string PositionLabel { get; set; } = "";

        /// <summary>
        ///     score of this player. is calculated as win rate * average enemy skill
        /// </summary>
        [ColumnMapping("score")]
        public float Score { get; set; }

        /// <summary>
        ///     how many plays this player has had at this position
        /// </summary>
        [ColumnMapping("play_count")]
        public int PlayCount { get; set; }

        /// <summary>
        ///     how many wins this player has had at this position
        /// </summary>
        [ColumnMapping("win_count")]
        public int WinCount { get; set; }

        /// <summary>
        ///     the average enemy skill across all games
        /// </summary>
        [ColumnMapping("average_enemy_skill")]
        public float AverageEnemySkill { get; set; }

        /// <summary>
        ///     timestamp of when this data was generated
        /// </summary>
        [ColumnMapping("timestamp")]
        public DateTime Timestamp { get; set; }

        /// <summary>
        ///     populated from the API controller
        /// </summary>
        public BarUser? User { get; set; } = null;

    }
}
