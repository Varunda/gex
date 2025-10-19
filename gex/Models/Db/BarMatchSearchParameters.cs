using gex.Code.Constants;
using System;
using System.Collections.Generic;

namespace gex.Models.Db {

    /// <summary>
    ///		search parameters when searching the database. If an option is null, it is not used
    /// </summary>
    public class BarMatchSearchParameters {

        /// <summary>
        ///		exact match to <see cref="BarMatch.Engine"/>
        /// </summary>
        public string? EngineVersion { get; set; }

        /// <summary>
        ///		exact match to <see cref="BarMatch.GameVersion"/>
        /// </summary>
        public string? GameVersion { get; set; }

        /// <summary>
        ///		exact match to <see cref="BarMatch.Map"/>
        /// </summary>
        public string? Map { get; set; }

        /// <summary>
        ///		the value of <see cref="BarMatch.StartTime"/> must come AFTER this value
        /// </summary>
        public DateTime? StartTimeAfter { get; set; }

        /// <summary>
        ///		the value of <see cref="BarMatch.StartTime"/> must come BEFORE this value
        /// </summary>
        public DateTime? StartTimeBefore { get; set; }

        /// <summary>
        ///		the value of <see cref="BarMatch.DurationMs"/> must be greater than this value
        /// </summary>
        public long? DurationMinimum { get; set; }

        /// <summary>
        ///		the value of <see cref="BarMatch.DurationMs"/> must be equal or less than this value
        /// </summary>
        public long? DurationMaximum { get; set; }

        /// <summary>
        ///		does the game have to be ranked or not?
        /// </summary>
        public bool? Ranked { get; set; }

        /// <summary>
        ///		gamemode, see <see cref="BarGamemode"/> for which values are what
        /// </summary>
        public byte? Gamemode { get; set; }

        /// <summary>
        ///		has this match been downloaded
        /// </summary>
        public bool? ProcessingDownloaded { get; set; }

        /// <summary>
        ///		has the demofile of this been parsed
        /// </summary>
        public bool? ProcessingParsed { get; set; }

        /// <summary>
        ///		has this match been replayed locally?
        /// </summary>
        public bool? ProcessingReplayed { get; set; }

        /// <summary>
        ///		has the action log of this match from a local replay been parsed?
        /// </summary>
        public bool? ProcessingAction { get; set; }

        /// <summary>
        ///		minimum number of players to be included
        /// </summary>
        public int? PlayerCountMinimum { get; set; }

        /// <summary>
        ///		minimum number of players to be included
        /// </summary>
        public int? PlayerCountMaximum { get; set; }

        /// <summary>
        ///		is legion enabled or disdabled
        /// </summary>
        public bool? LegionEnabled { get; set; }

        /// <summary>
        ///     id of the <see cref="MatchPool"/> to search for
        /// </summary>
        public long? PoolID { get; set; }

        public List<SearchKeyValue> GameSettings { get; set; } = [];

        public OrderBy OrderBy { get; set; } = OrderBy.DURATION;

        public OrderByDirection OrderByDirection { get; set; } = OrderByDirection.ASC;

        public static OrderBy? ParseOrderBy(string name) {
            return name.ToLower() switch {
                "start_time" => (OrderBy?)OrderBy.START_TIME,
                "player_count" => (OrderBy?)OrderBy.PLAYER_COUNT,
                "duration" => (OrderBy?)OrderBy.DURATION,
                _ => null,
            };
        }

        public static OrderByDirection? ParseOrderByDirection(string name) {
            return name.ToLower() switch {
                "asc" => (OrderByDirection?)OrderByDirection.ASC,
                "desc" => (OrderByDirection?)OrderByDirection.DESC,
                _ => null
            };
        }

    }

    /// <summary>
    ///     possible values for the field being ordered during a search
    /// </summary>
    public class OrderBy {

        public readonly string Value;

        private OrderBy(string value) {
            Value = value;
        }

        public readonly static OrderBy START_TIME = new("start_time");

        public readonly static OrderBy PLAYER_COUNT = new("player_count");

        public readonly static OrderBy DURATION = new("duration_ms");

    }

    /// <summary>
    ///     possible values for the order by direction during a search
    /// </summary>
    public class OrderByDirection {

        public readonly string Value;

        private OrderByDirection(string value) {
            Value = value;
        }

        public readonly static OrderByDirection ASC = new("ASC");

        public readonly static OrderByDirection DESC = new("DESC");

    }

}
