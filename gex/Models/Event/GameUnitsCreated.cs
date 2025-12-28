using Dapper.ColumnMapper;
using gex.Code;
using gex.Models.Db;
using gex.Models.UserStats;
using gex.Services.Repositories;
using System;

namespace gex.Models.Event {

    /// <summary>
    ///     aggregate count of units made within a game
    /// </summary>
    [DapperColumnsMapped]
    public class GameUnitsCreated {

        /// <summary>
        ///     id of the <see cref="BarMatch"/>
        /// </summary>
        [ColumnMapping("game_id")]
        public string GameID { get; set; } = "";

        /// <summary>
        ///     id of the <see cref="BarMatchPlayer"/> this count is for
        /// </summary>
        [ColumnMapping("team_id")]
        public short TeamID { get; set; }

        /// <summary>
        ///     id of the <see cref="BarUser"/> this count is for
        /// </summary>
        [ColumnMapping("user_id")]
        public long UserID { get; set; }

        /// <summary>
        ///     unit's definition name
        /// </summary>
        [ColumnMapping("definition_name")]
        public string DefinitionName { get; set; } = "";

        /// <summary>
        ///     english name of the unit based on the <see cref="DefinitionName"/>. is null when loaded from the DB,
        ///     load using a <see cref="BarI18nRepository"/>
        /// </summary>
        public string? UnitName { get; set; } = null;

        /// <summary>
        ///     how many of this unit were made over the whole game
        /// </summary>
        [ColumnMapping("count")]
        public int Count { get; set; }

        /// <summary>
        ///     when this data was generated
        /// </summary>
        [ColumnMapping("timestamp")]
        public DateTime Timestamp { get; set; }

    }
}
