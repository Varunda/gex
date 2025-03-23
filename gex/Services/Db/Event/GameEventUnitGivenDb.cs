using gex.Code.ExtensionMethods;
using gex.Models.Event;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace gex.Services.Db.Event {

    public class GameEventUnitGivenDb : BaseGameEventDb<GameEventUnitGiven> {

        public GameEventUnitGivenDb(ILoggerFactory loggerFactory, IDbHelper dbHelper)
            : base("game_event_unit_given", "unit_given", loggerFactory, dbHelper) { }

        protected override void SetupInsert(GameEventUnitGiven ev, NpgsqlCommand cmd) {
            cmd.CommandText = @"
                INSERT INTO game_event_unit_given (
                    game_id, frame,
                    unit_id, team_id, new_team_id, definition_id,
                    unit_x, unit_y, unit_z
                ) VALUES (
                    @GameID, @Frame,
                    @UnitID, @TeamID, @NewTeamID, @DefinitionID,
                    @UnitX, @UnitY, @UnitZ
                );
            ";

            cmd.AddParameter("GameID", ev.GameID);
            cmd.AddParameter("Frame", ev.Frame);
            cmd.AddParameter("UnitID", ev.UnitID);
            cmd.AddParameter("TeamID", ev.TeamID);
            cmd.AddParameter("NewTeamID", ev.NewTeamID);
            cmd.AddParameter("DefinitionID", ev.DefinitionID);
            cmd.AddParameter("UnitX", ev.UnitX);
            cmd.AddParameter("UnitY", ev.UnitY);
            cmd.AddParameter("UnitZ", ev.UnitZ);
        }

    }
}
