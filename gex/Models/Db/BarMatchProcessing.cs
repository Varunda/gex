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

    }
}
