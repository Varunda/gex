using gex.Code.ExtensionMethods;
using gex.Models.Event;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.Event {

    public class GameUnitsCreatedDb {

        private readonly ILogger<GameUnitsCreatedDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public GameUnitsCreatedDb(ILogger<GameUnitsCreatedDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        public async Task<List<GameUnitsCreated>> GetByGameID(string gameID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.EVENT);
            return await conn.QueryListAsync<GameUnitsCreated>(
                "SELECT * FROM game_units_created WHERE game_id = @GameID",
                new { GameID = gameID },
                cancel
            );
        }

        public async Task<List<GameUnitsCreated>> GetByUserID(long userID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.EVENT);
            return await conn.QueryListAsync<GameUnitsCreated>(
                "SELECT * FROM game_units_created WHERE user_id = @UserID",
                new { UserID = userID },
                cancel
            );
        }

        /// <summary>
        ///     generate the <see cref="GameUnitsCreated"/> for a specific game
        /// </summary>
        /// <param name="gameID">ID of the game</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns>a task for the async operation</returns>
        public async Task Generate(string gameID, CancellationToken cancel) {
            _Logger.LogDebug($"generating game units created [gameID={gameID}]");

            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.EVENT);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO game_units_created (
                    game_id, team_id, user_id, definition_name, count, timestamp
                ) SELECT
                    uc.game_id, uc.team_id, mp.user_id, uc.definition_name, count(*), NOW() at time zone 'UTC'
                FROM game_event_unit_created uc
                    INNER JOIN bar_match_player mp ON uc.game_id = mp.game_id AND uc.team_id = mp.team_id
                WHERE uc.game_id = @GameID
                GROUP BY 1, 2, 3, 4;
            ", cancel);

            cmd.AddParameter("GameID", gameID);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

        /// <summary>
        ///     generate the <see cref="GameUnitsCreated"/> for all games 
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task GenerateAllMissing(CancellationToken cancel) {
            _Logger.LogDebug($"generating game_units_created data for all games without one");

            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.EVENT);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                WITH missing_games AS (
                    SELECT m.id
                    FROM bar_match m
                        left join game_units_created uc ON uc.game_id = m.id
                        left join bar_match_processing mp ON m.id = mp.game_id
                    WHERE uc.game_id is null
                        AND mp.action_ms IS NOT NULL
                )
                INSERT INTO game_units_created (
                    game_id, team_id, user_id, definition_name, count, timestamp
                ) SELECT
                    uc.game_id, uc.team_id, mp.user_id, uc.definition_name, count(*), NOW() at time zone 'UTC'
                FROM game_event_unit_created uc
                    INNER JOIN bar_match_player mp ON uc.game_id = mp.game_id AND uc.team_id = mp.team_id
                WHERE uc.game_id IN (select id from missing_games)
                GROUP BY 1, 2, 3, 4;
            ", cancel);

            cmd.CommandTimeout = 60 * 60 * 8; // 8 hours

            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

    }
}
