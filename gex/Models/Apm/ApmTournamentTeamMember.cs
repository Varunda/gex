using gex.Common.Code.ExtensionMethods;
using System.Text.Json;

namespace gex.Models.Apm {

    public class ApmTournamentTeamMember {

        public string ID { get; set; } = "";

        public long BarUserID { get; set; } = 0;

        public string Name { get; set; } = "";

        public static ApmTournamentTeamMember Parse(JsonElement json) {
            ApmTournamentTeamMember member = new();

            member.ID = json.GetRequiredString("user_id");
            member.BarUserID = json.GetRequiredInt64("bar_player_id");
            member.Name = json.GetRequiredString("player_name");

            return member;
        }

    }
}
