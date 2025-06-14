﻿using gex.Code.ExtensionMethods;
using gex.Models.Event;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace gex.Services.Db.Event {

    public class GameEventUnitCreatedDb : BaseGameEventDb<GameEventUnitCreated> {

        public GameEventUnitCreatedDb(ILoggerFactory loggerFactory, IDbHelper dbHelper)
            : base("game_event_unit_created", "unit_created", loggerFactory, dbHelper) { }

        protected override void SetupInsert(GameEventUnitCreated ev, NpgsqlCommand cmd) {
            cmd.CommandText = @"
                INSERT INTO game_event_unit_created (
                    game_id, frame, unit_id, team_id, definition_id,
                    unit_x, unit_y, unit_z, rotation
                ) VALUES (
                    @GameID, @Frame, @UnitID, @TeamID, @DefinitionID,
                    @UnitX, @UnitY, @UnitZ, @Rotation
                );
            ";

            cmd.AddParameter("GameID", ev.GameID);
            cmd.AddParameter("Frame", ev.Frame);
            cmd.AddParameter("UnitID", ev.UnitID);
            cmd.AddParameter("TeamID", ev.TeamID);
            cmd.AddParameter("DefinitionID", ev.DefinitionID);
            cmd.AddParameter("UnitX", ev.UnitX);
            cmd.AddParameter("UnitY", ev.UnitY);
            cmd.AddParameter("UnitZ", ev.UnitZ);
            cmd.AddParameter("Rotation", ev.Rotation);
        }

    }
}
