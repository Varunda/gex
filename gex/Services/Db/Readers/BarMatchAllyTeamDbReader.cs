using gex.Models.Db;
using Npgsql;
using System.Data;

namespace gex.Services.Db.Readers {

    public class BarMatchAllyTeamDbReader : IDataReader<BarMatchAllyTeam> {

        public override BarMatchAllyTeam? ReadEntry(NpgsqlDataReader reader) {
            BarMatchAllyTeam allyTeam = new();

            allyTeam.GameID = reader.GetString("game_id");
            allyTeam.AllyTeamID = reader.GetInt32("ally_team_id");
            allyTeam.PlayerCount = reader.GetInt32("player_count");
            allyTeam.StartBox.Top = reader.GetFloat("start_box_top");
            allyTeam.StartBox.Bottom = reader.GetFloat("start_box_bottom");
            allyTeam.StartBox.Left = reader.GetFloat("start_box_left");
            allyTeam.StartBox.Right = reader.GetFloat("start_box_right");

            return allyTeam;
        }

    }
}
