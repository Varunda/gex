using gex.Common.Code.ExtensionMethods;
using System.Collections.Generic;
using System.Text.Json;

namespace gex.Models.Apm {

    public class ApmTournamentTeam {

        public string ID { get; set; } = "";

        public string Name { get; set; } = "";

        public string TeamName { get; set; } = "";

        public List<ApmTournamentTeamMember> Members { get; set; } = [];

        public static ApmTournamentTeam Parse(JsonElement json) {
            ApmTournamentTeam team = new();

            team.ID = json.GetRequiredString("id");
            team.Name = json.GetRequiredString("name");
            team.TeamName = json.GetRequiredString("team_name");

            JsonElement entries = json.GetRequiredChild("members");
            foreach (JsonElement entry in entries.EnumerateArray()) {
                team.Members.Add(ApmTournamentTeamMember.Parse(entry));
            }

            return team;
        }

    }
}
