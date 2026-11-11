using gex.Common.Code.ExtensionMethods;
using System;
using System.Text.Json;

namespace gex.Models.Apm {

    public class ApmTournamentMatchEntry {

        public string EntryID { get; set; } = "";

        public int? Score { get; set; }

        public static ApmTournamentMatchEntry Parse(JsonElement json) {
            ApmTournamentMatchEntry entry = new();

            entry.EntryID = json.GetRequiredString("entry_id");

            JsonElement score = json.GetRequiredChild("score");
            if (score.ValueKind == JsonValueKind.Number) {
                entry.Score = score.GetInt32();
            } else if (score.ValueKind == JsonValueKind.Null) {
                entry.Score = null;
            } else {
                throw new InvalidOperationException($"unhandled ValueKind for 'score': {score.ValueKind}");
            }

            return entry;
        }

    }
}
