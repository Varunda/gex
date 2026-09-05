using gex.Common.Models.Match;

namespace gex.Models.Queues {

    public class GameReplayParseQueueEntry {

        public string GameID { get; set; } = "";

        public bool Force { get; set; } = false;

        /// <summary>
        ///     will this entry be put in the next queue, even if the <see cref="BarMatchProcessing"/>
        ///     for it already has the next step complete?
        /// </summary>
        public bool ForceForward { get; set; }

        /// <summary>
        ///     will the entry not re-generate map and user stats when reparsed? useful for mass re-parsing
        /// </summary>
        public bool SkipStatUpdates { get; set; } = false;

        /// <summary>
        ///     will the entry when parsed not be sent thru any webhooks? useful for mass re-parsing
        /// </summary>
        public bool SkipWebhook { get; set; } = false;

    }
}
