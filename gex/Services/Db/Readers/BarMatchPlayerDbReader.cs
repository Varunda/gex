using gex.Code.ExtensionMethods;
using gex.Models.Db;
using Npgsql;
using System.Data;

namespace gex.Services.Db.Readers {

    public class BarMatchPlayerDbReader : IDataReader<BarMatchPlayer> {

        public override BarMatchPlayer? ReadEntry(NpgsqlDataReader reader) {
            BarMatchPlayer player = new();

            player.GameID = reader.GetString("game_id");
            player.PlayerID = reader.GetInt32("player_id");
            player.UserID = reader.GetInt64("user_id");
            player.Name = reader.GetString("user_name");
            player.TeamID = reader.GetInt32("team_id");
            player.AllyTeamID = reader.GetInt32("ally_team_id");
            player.Skill = reader.GetDouble("skill");
            player.SkillUncertainty = reader.GetDouble("skill_uncertainty");

            return player;
        }

    }
}
