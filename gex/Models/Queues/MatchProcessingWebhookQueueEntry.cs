using System;

namespace gex.Models.Queues {

    public class MatchProcessingWebhookQueueEntry {

        public string GameID { get; set; } = "";

        public string Type { get; private set; } = "";

        public const string PARSED = "parsed";

        public const string REPLAYED = "replayed";

        public static MatchProcessingWebhookQueueEntry Parsed(string gameID) {
            return new MatchProcessingWebhookQueueEntry() {
                GameID = gameID,
                Type = PARSED
            };
        }

        public static MatchProcessingWebhookQueueEntry Replayed(string gameID) {
            return new MatchProcessingWebhookQueueEntry() {
                GameID = gameID,
                Type = REPLAYED
            };
        }

        private MatchProcessingWebhookQueueEntry() { }

    }
}
