using gex.Code.ExtensionMethods;
using gex.Models.Db;
using Npgsql;
using System.Data;

namespace gex.Services.Db.Readers {

    public class BarMatchTeamDbReader : IDataReader<BarMatchTeam> {

        public override BarMatchTeam? ReadEntry(NpgsqlDataReader reader) {
            BarMatchTeam team = new();

            team.GameID = reader.GetString("game_id");
            team.TeamID = reader.GetInt32("team_id");
            team.AllyTeamID = reader.GetInt32("ally_team_id");
            team.Faction = reader.GetString("faction");
            team.TeamLeaderID = reader.GetInt32("team_leader_id");
            team.StartingPosition = new System.Numerics.Vector3() {
                X = reader.GetFloat("starting_position_x"),
                Y = reader.GetFloat("starting_position_y"),
                Z = reader.GetFloat("starting_position_z"),
            };
            team.Color = reader.GetInt32("color");
            team.Handicap = reader.GetFloat("handicap");
            team.StartSpot = reader.GetNullableString("start_spot");
            team.StartSpotLabel = reader.GetNullableString("start_spot_label");

            return team;
        }

    }
}
