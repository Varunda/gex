using Dapper;
using gex.Code.ExtensionMethods;
using gex.Models.Event;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace gex.Services.Db {

    public class GameEventUnitKilledDb {

        private readonly ILogger<GameEventUnitKilledDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public GameEventUnitKilledDb(ILogger<GameEventUnitKilledDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        public async Task Insert(GameEventUnitKilled ev) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO game_event_unit_killed (
                    game_id, frame, unit_id, team_id, definition_id,
                    attacker_id, attacker_team, attacker_definition_id, weapon_definition_id,
                    killed_x, killed_y, killed_z,
                    attacker_x, attacker_y, attacker_z
                ) VALUES (
                    @GameID, @Frame, @UnitID, @TeamID, @DefinitionID,
                    @AttackerID, @AttackerTeam, @AttackerDefID, @WeaponDefID,
                    @KilledX, @KilledY, @KilledZ,
                    @AttackerX, @AttackerY, @AttackerZ
                ) ON CONFLICT (game_id, frame, unit_id) DO NOTHING;
            ");

            cmd.AddParameter("GameID", ev.GameID);
            cmd.AddParameter("Frame", ev.Frame);
            cmd.AddParameter("UnitID", ev.UnitID);
            cmd.AddParameter("TeamID", ev.TeamID);
            cmd.AddParameter("DefinitionID", ev.DefinitionID);
            cmd.AddParameter("AttackerID", ev.AttackerID);
            cmd.AddParameter("AttackerTeam", ev.AttackerTeam);
            cmd.AddParameter("AttackerDefID", ev.AttackerDefinitionID);
            cmd.AddParameter("WeaponDefID", ev.WeaponDefinitionID);
            cmd.AddParameter("KilledX", ev.KilledX);
            cmd.AddParameter("KilledY", ev.KilledY);
            cmd.AddParameter("KilledZ", ev.KilledZ);
            cmd.AddParameter("AttackerX", ev.AttackerX);
            cmd.AddParameter("AttackerY", ev.AttackerY);
            cmd.AddParameter("AttackerZ", ev.AttackerZ);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

        public async Task<List<GameEventUnitKilled>> GetByGameID(string gameID) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            List<GameEventUnitKilled> evs = (await conn.QueryAsync<GameEventUnitKilled>(
                "SELECT 'unit_killed' \"Action\", * from game_event_unit_killed WHERE game_id = @GameID",
                new { GameID = gameID }
            )).ToList();

            await conn.CloseAsync();

            return evs;
        }

    }
}
