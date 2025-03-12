using System;

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


    }
}
