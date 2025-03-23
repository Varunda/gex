using gex.Code.ExtensionMethods;
using gex.Models.Event;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace gex.Services.Db.Event {

    public class GameEventTransportUnloadedDb : BaseGameEventDb<GameEventUnitTransportUnloaded> {

        public GameEventTransportUnloadedDb(ILoggerFactory loggerFactory, IDbHelper dbHelper)
            : base("game_event_unit_transport_unloaded", "transport_unloaded", loggerFactory, dbHelper) { }

        protected override void SetupInsert(GameEventUnitTransportUnloaded ev, NpgsqlCommand cmd) {
            cmd.CommandText = @"
                INSERT INTO game_event_unit_transport_unloaded (
                    game_id, frame,
                    unit_id, team_id, definition_id,
                    transport_unit_id, transport_team_id,
                    unit_x, unit_y, unit_z
                ) VALUES (
                    @GameID, @Frame,
                    @UnitID, @TeamID, @DefinitionID,
                    @TransportID, @TransportTeamID,
                    @UnitX, @UnitY, @UnitZ
                );
            ";

            cmd.AddParameter("GameID", ev.GameID);
            cmd.AddParameter("Frame", ev.Frame);
            cmd.AddParameter("UnitID", ev.UnitID);
            cmd.AddParameter("TeamID", ev.TeamID);
            cmd.AddParameter("DefinitionID", ev.DefinitionID);
            cmd.AddParameter("TransportID", ev.TransportUnitID);
            cmd.AddParameter("TransportTeamID", ev.TransportTeamID);
            cmd.AddParameter("UnitX", ev.UnitX);
            cmd.AddParameter("UnitY", ev.UnitY);
            cmd.AddParameter("UnitZ", ev.UnitZ);
        }

    }
}
