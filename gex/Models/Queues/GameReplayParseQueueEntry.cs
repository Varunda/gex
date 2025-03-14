namespace gex.Models.Queues {

    public class GameReplayParseQueueEntry {

        public string GameID { get; set; } = "";

        public string FileName { get; set; } = "";

        public bool Force { get; set; } = false;

        /// <summary>
        ///     will this entry be put in the next queue, even if the <see cref="BarMatchProcessing"/>
        ///     for it already has the next step complete?
        /// </summary>
        public bool ForceForward { get; set; }

    }
}
