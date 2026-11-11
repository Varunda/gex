using gex.Common.Code.ExtensionMethods;
using System.Collections.Generic;
using System.Text.Json;

namespace gex.Models.Apm {

    public class ApmTournamentMatch {

        public string ID { get; set; } = "";

        public string RoundID { get; set; } = "";

        public int Round { get; set; }

        public int MatchNumber { get; set; }

        public int BestOf { get; set; }

        public List<ApmTournamentMatchEntry> Entries { get; set; } = [];

        public string? WinnerEntryID { get; set; } = null;

        public List<ApmTournamentMatchGame> Games { get; set; } = [];

        public static ApmTournamentMatch Parse(JsonElement json) {
            ApmTournamentMatch match = new();

            match.ID = json.GetRequiredString("id");
            match.Round = json.GetRequiredChild("round").GetInt32();
            match.MatchNumber = json.GetRequiredChild("match_number").GetInt32();
            match.RoundID = json.GetRequiredString("round_id");
            match.BestOf = json.GetRequiredChild("best_of").GetInt32();
            match.WinnerEntryID = json.GetRequiredString("winner_entry_id");

            JsonElement entries = json.GetRequiredChild("entries");
            foreach (JsonElement entry in entries.EnumerateArray()) {
                match.Entries.Add(ApmTournamentMatchEntry.Parse(entry));
            }

            JsonElement games = json.GetRequiredChild("games");
            foreach (JsonElement game in games.EnumerateArray()) {
                match.Games.Add(ApmTournamentMatchGame.Parse(game));
            }

            return match;
        }

    }
}
