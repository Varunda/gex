using gex.Common.Models.Match;
using gex.Common.Services.Db;
using Npgsql;
using System.Data.Common;
using gex.Common.Code.ExtensionMethods;
using System.Data;

namespace gex.Services.Db.Readers {

    public class BarMatchAllyTeamDbReader : IDataReader<BarMatchAllyTeam> {

        public override BarMatchAllyTeam? ReadEntry(DbDataReader reader) {
            BarMatchAllyTeam allyTeam = new();

            allyTeam.GameID = reader.GetString("game_id");
            allyTeam.AllyTeamID = reader.GetInt32("ally_team_id");
            allyTeam.PlayerCount = reader.GetInt32("player_count");
            allyTeam.Won = reader.GetBoolean("won");
            allyTeam.StartBox.Top = reader.GetFloat("start_box_top");
            allyTeam.StartBox.Bottom = reader.GetFloat("start_box_bottom");
            allyTeam.StartBox.Left = reader.GetFloat("start_box_left");
            allyTeam.StartBox.Right = reader.GetFloat("start_box_right");
            allyTeam.AverageSkill = reader.GetFloat("average_skill");

            return allyTeam;
        }

    }
}
