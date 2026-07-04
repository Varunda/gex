using gex.Common.Code.Constants;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text.Json;

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

        /// <summary>
        ///     list of game settings to filter for
        /// </summary>
        public List<SearchKeyValue> GameSettings { get; set; } = [];

        /// <summary>
        ///     list of user IDs to search for. if empty, no filter is applied
        /// </summary>
        public List<long> UserIDs { get; set; } = [];

        /// <summary>
        ///     list of player filters to seach for
        /// </summary>
        public List<SearchPlayer> Players { get; set; } = [];

        /// <summary>
        ///     minimum OS of all players
        /// </summary>
        public double? MinimumOS { get; set; }

        /// <summary>
        ///     maximum OS of all players
        /// </summary>
        public double? MaximumOS { get; set; }

        /// <summary>
        ///     minimum average OS
        /// </summary>
        public double? MinimumAverageOS { get; set; }

        /// <summary>
        ///     maximum average OS
        /// </summary>
        public double? MaximumAverageOS { get; set; }

        /// <summary>
        ///     show games replayed after this time
        /// </summary>
        public DateTime? ReplayedAfter { get; set; }

        /// <summary>
        ///     show games replayed before this time
        /// </summary>
        public DateTime? ReplayedBefore { get; set; }

        public OrderBy OrderBy { get; set; } = OrderBy.START_TIME;

        public OrderByDirection OrderByDirection { get; set; } = OrderByDirection.DESC;

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

        public readonly static OrderBy DURATION = new("duration_frame_count");

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

    public class SearchPlayer : IParsable<SearchPlayer> {

        public long? UserID { get; set; } = null;

        public Vector3? Position { get; set; }

        public float? PositionRadius { get; set; }

        public string? PositionLabel { get; set; } = null;

        public float? MinOS { get; set; }

        public float? MaxOS { get; set; }

        public static SearchPlayer Parse(string s, IFormatProvider? provider) {
            if (TryParse(s, provider, out SearchPlayer? result) == false) {
                throw new FormatException();
            }
            return result;
        }

        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out SearchPlayer result) {
            if (s == null) {
                result = null;
                return false;
            }

            try {
                SearchPlayer? player = JsonSerializer.Deserialize<SearchPlayer>(s, new JsonSerializerOptions() {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                result = player;
                return player != null;
            } catch (Exception) {
                result = null;
                return false;
            }
        }
    }

}
