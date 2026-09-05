using gex.Code.ExtensionMethods;
using gex.Common.Services.Db;
using gex.Models.Event;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data.Common;
using gex.Common.Code.ExtensionMethods;

namespace gex.Services.Db.Event {

    public class GameEventFactoryUnitCreatedDb : BaseGameEventDb<GameEventFactoryUnitCreated> {

        public GameEventFactoryUnitCreatedDb(ILoggerFactory loggerFactory, IDbHelper dbHelper)
            : base("game_event_factory_unit_created", "factory_unit_created", loggerFactory, dbHelper) { }

        protected override void SetupInsert(GameEventFactoryUnitCreated ev, DbCommand cmd) {
            cmd.CommandText = @"
                INSERT INTO game_event_factory_unit_created (
                    game_id, frame,
                    unit_id, team_id, definition_id,
                    factory_unit_id, factory_definition_id
                ) VALUES (
                    @GameID, @Frame,
                    @UnitID, @TeamID, @DefinitionID,
                    @FactoryUnitID, @FactoryDefinitionID
                );
            ";

            cmd.AddParameter("GameID", ev.GameID);
            cmd.AddParameter("Frame", ev.Frame);
            cmd.AddParameter("UnitID", ev.UnitID);
            cmd.AddParameter("TeamID", ev.TeamID);
            cmd.AddParameter("DefinitionID", ev.DefinitionID);
            cmd.AddParameter("FactoryUnitID", ev.FactoryUnitID);
            cmd.AddParameter("FactoryDefinitionID", ev.FactoryDefinitionID);
        }

    }
}
