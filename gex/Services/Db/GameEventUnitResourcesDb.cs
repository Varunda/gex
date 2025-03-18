using gex.Code.ExtensionMethods;
using gex.Models.Event;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace gex.Services.Db {

    public class GameEventUnitResourcesDb : BaseGameEventDb<GameEventUnitResources> {

        public GameEventUnitResourcesDb(ILoggerFactory loggerFactory, IDbHelper dbHelper)
            : base("game_event_unit_resources", "unit_resources", loggerFactory, dbHelper) { }

        protected override void SetupInsert(GameEventUnitResources ev, NpgsqlCommand cmd) {
            cmd.CommandText = @"
                INSERT INTO game_event_unit_resources (
                    game_id, frame,
                    unit_id, team_id, definition_id,
                    metal_made, metal_used, energy_made, energy_used
                ) VALUES (
                    @GameID, @Frame,
                    @UnitID, @TeamID, @DefinitionID,
                    @MetalMade, @MetalUsed, @EnergyMade, @EnergyUsed
                );
            ";

            cmd.AddParameter("GameID", ev.GameID);
            cmd.AddParameter("Frame", ev.Frame);
            cmd.AddParameter("UnitID", ev.UnitID);
            cmd.AddParameter("TeamID", ev.TeamID);
            cmd.AddParameter("DefinitionID", ev.DefinitionID);
            cmd.AddParameter("MetalMade", ev.MetalMade);
            cmd.AddParameter("MetalUsed", ev.MetalUsed);
            cmd.AddParameter("EnergyMade", ev.EnergyMade);
            cmd.AddParameter("EnergyUsed", ev.EnergyUsed);
        }

    }
}
