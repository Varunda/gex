using Dapper;
using gex.Code.ExtensionMethods;
using gex.Models.Event;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace gex.Services.Db {

    public class GameEventUnitCreatedDb {

        private readonly ILogger<GameEventUnitCreatedDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public GameEventUnitCreatedDb(ILogger<GameEventUnitCreatedDb> logger, IDbHelper dbHelper) {
            _Logger = logger;
            _DbHelper = dbHelper;
        }

        public async Task Insert(GameEventUnitCreated ev) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO game_event_unit_created (
                    game_id, frame, unit_id, team_id, definition_id,
                    unit_x, unit_y, unit_z
                ) VALUES (
                    @GameID, @Frame, @UnitID, @TeamID, @DefinitionID,
                    @UnitX, @UnitY, @UnitZ
                ) ON CONFLICT (game_id, unit_id, frame) DO NOTHING;
            ");

            cmd.AddParameter("GameID", ev.GameID);
            cmd.AddParameter("Frame", ev.Frame);
            cmd.AddParameter("UnitID", ev.UnitID);
            cmd.AddParameter("TeamID", ev.TeamID);
            cmd.AddParameter("DefinitionID", ev.DefinitionID);
            cmd.AddParameter("UnitX", ev.UnitX);
            cmd.AddParameter("UnitY", ev.UnitY);
            cmd.AddParameter("UnitZ", ev.UnitZ);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

        public async Task<List<GameEventUnitCreated>> GetByGameID(string gameID) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            List<GameEventUnitCreated> evs = (await conn.QueryAsync<GameEventUnitCreated>(
                "SELECT 'unit_created' \"Action\", * from game_event_unit_created WHERE game_id = @GameID",
                new { GameID = gameID }
            )).ToList();

            await conn.CloseAsync();

            return evs;
        }

    }
}
