using gex.Code.ExtensionMethods;
using gex.Models.Event;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace gex.Services.Db {

    public class GameEventTeamDiedDb : BaseGameEventDb<GameEventTeamDied> {

        public GameEventTeamDiedDb(ILoggerFactory loggerFactory, IDbHelper dbHelper) 
            : base("game_event_team_died", "team_died", loggerFactory, dbHelper) { }

        protected override void SetupInsert(GameEventTeamDied ev, NpgsqlCommand cmd) {
            cmd.CommandText = @"
                INSERT INTO game_event_team_died (
                    game_id, frame, team_id
                ) VALUES (
                    @GameID, @Frame, @TeamID
                );
            ";

            cmd.AddParameter("GameID", ev.GameID);
            cmd.AddParameter("Frame", ev.Frame);
            cmd.AddParameter("TeamID", ev.TeamID);
        }
    }
}
