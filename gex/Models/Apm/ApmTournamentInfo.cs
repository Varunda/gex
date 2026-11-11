using gex.Common.Code.ExtensionMethods;
using System.Collections.Generic;
using System.Text.Json;

namespace gex.Models.Apm {

    public class ApmTournamentInfo {

        public string ID { get; set; } = "";

        public string Name { get; set; } = "";

        public string Slug { get; set; } = "";

        public long? GexMatchPoolID { get; set; }

        public List<ApmTournamentRound> Rounds { get; set; } = [];

        public List<ApmTournamentTeam> Teams { get; set; } = [];

        public List<ApmTournamentMatch> Matches { get; set; } = [];

        public static ApmTournamentInfo Parse(JsonElement json) {
            ApmTournamentInfo info = new();

            info.ID = json.GetRequiredString("id");
            info.Name = json.GetRequiredString("name");
            info.Slug = json.GetRequiredString("slug");

            JsonElement poolID = json.GetRequiredChild("gex_match_pool_id");
            if (poolID.ValueKind != JsonValueKind.Null) {
                info.GexMatchPoolID = long.Parse(poolID.GetString()!);
            }

            JsonElement rounds = json.GetRequiredChild("rounds");
            foreach (JsonElement entry in rounds.EnumerateArray()) {
                info.Rounds.Add(ApmTournamentRound.Parse(entry));
            }

            JsonElement entries = json.GetRequiredChild("entries");
            foreach (JsonElement entry in entries.EnumerateArray()) {
                info.Teams.Add(ApmTournamentTeam.Parse(entry));
            }

            JsonElement matches = json.GetRequiredChild("matches");
            foreach (JsonElement match in matches.EnumerateArray()) {
                info.Matches.Add(ApmTournamentMatch.Parse(match));
            }

            return info;
        }

    }
}
