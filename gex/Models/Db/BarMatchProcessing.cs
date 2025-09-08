using Dapper.ColumnMapper;
using gex.Code;
using System;

namespace gex.Models.Db {

    /// <summary>
    ///     represents the various processing steps taken on a bar replay, and what actions have been performed
    /// </summary>
    [DapperColumnsMapped]
    public class BarMatchProcessing {

        /// <summary>
        ///     ID of the <see cref="BarMatch"/> this processing entry is for
        /// </summary>
        [ColumnMapping("game_id")]
        public string GameID { get; set; } = "";

        /// <summary>
        ///     timestamp of when the replay file was successfully downloaded
        /// </summary>
        [ColumnMapping("demofile_fetched")]
        public DateTime? ReplayDownloaded { get; set; }

        /// <summary>
        ///     timestamp of when the replay file was successfully parsed
        /// </summary>
        [ColumnMapping("demofile_parsed")]
        public DateTime? ReplayParsed { get; set; }

        /// <summary>
        ///     timestamp of when the replay file was successfully simulated locally
        /// </summary>
        [ColumnMapping("headless_ran")]
        public DateTime? ReplaySimulated { get; set; }

        /// <summary>
        ///     timestamp of when the action log produced by local playback was successfully processed
        /// </summary>
        [ColumnMapping("actions_parsed")]
        public DateTime? ActionsParsed { get; set; }

        /// <summary>
        ///     how long it took the replay to be downloaded
        /// </summary>
        [ColumnMapping("fetch_ms")]
        public int? ReplayDownloadedMs { get; set; }

        /// <summary>
        ///     how long it took the replay to be parsed
        /// </summary>
        [ColumnMapping("parse_ms")]
        public int? ReplayParsedMs { get; set; }

        /// <summary>
        ///     how long it took to simulate the demofile locally
        /// </summary>
        [ColumnMapping("replay_ms")]
        public int? ReplaySimulatedMs { get; set; }

        /// <summary>
        ///     how long it took to parse the action log from simulating the game locally
        /// </summary>
        [ColumnMapping("action_ms")]
        public int? ActionsParsedMs { get; set; }

        /// <summary>
        ///		represents how quickly Gex will prioritize processing this game. lower means higher priority.
        ///		a negative priority	means this game will be processed right away
        /// </summary>
        [ColumnMapping("priority")]
        public short Priority { get; set; }

        /// <summary>
        ///     has the unit position data for this match been compressed?
        /// </summary>
        [ColumnMapping("unit_position_compressed")]
        public bool UnitPositionCompressed { get; set; }

    }
}
