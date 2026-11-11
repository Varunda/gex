using gex.Common.Code.ExtensionMethods;
using System.Text.Json;

namespace gex.Models.Apm {

    public class ApmTournamentRound {

        public string ID { get; set; } = "";

        public int Round { get; set; } = 0;

        public int BestOf { get; set; } = 0;

        public string? Note { get; set; } = null;

        public static ApmTournamentRound Parse(JsonElement json) {
            ApmTournamentRound round = new();

            round.ID = json.GetRequiredString("id");
            round.Round = json.GetRequiredChild("round_number").GetInt32();
            round.BestOf = json.GetRequiredChild("best_of").GetInt32();
            round.Note = json.GetRequiredChild("note").GetString();

            return round;
        }

    }
}
