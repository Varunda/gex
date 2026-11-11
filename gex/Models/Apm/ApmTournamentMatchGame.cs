using gex.Common.Code.ExtensionMethods;
using System;
using System.Text.Json;

namespace gex.Models.Apm {

    public class ApmTournamentMatchGame {

        public string ID { get; set; } = "";

        public int GameNumber { get; set; }

        public DateTime StartedAt { get; set; }

        public string WinnerEntryID { get; set; } = "";

        public string ReplayUrl { get; set; } = "";

        public static ApmTournamentMatchGame Parse(JsonElement elem) {
            ApmTournamentMatchGame game = new();

            game.ID = elem.GetRequiredString("gex_match_id");
            game.GameNumber = elem.GetRequiredChild("game_number").GetInt32();
            game.StartedAt = DateTime.Parse(elem.GetRequiredString("started_at"));
            game.WinnerEntryID = elem.GetRequiredString("winner_entry_id");
            game.ReplayUrl = elem.GetRequiredString("replay_url");

            return game;
        }

    }
}
