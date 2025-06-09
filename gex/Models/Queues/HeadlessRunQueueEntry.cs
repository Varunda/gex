using gex.Models.Db;

namespace gex.Models.Queues {

    /// <summary>
    ///     represents a queue of games to be ran headless-ly
    /// </summary>
    public class HeadlessRunQueueEntry {

        /// <summary>
        ///     ID of the game to run headlessly
        /// </summary>
        public string GameID { get; set; } = "";

        public bool Force { get; set; }

        /// <summary>
        ///     will this entry be put in the next queue, even if the <see cref="BarMatchProcessing"/>
        ///     for it already has the next step complete?
        /// </summary>
        public bool ForceForward { get; set; }


    }
}
