namespace gex.Models.Queues {

    public class GameReplayParseQueueEntry {

        public string GameID { get; set; } = "";

        public string FileName { get; set; } = "";

        public bool Force { get; set; } = false;

    }
}
