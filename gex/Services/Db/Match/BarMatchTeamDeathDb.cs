using gex.Code.ExtensionMethods;
using gex.Models.Db;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.Match {

    public class BarMatchTeamDeathDb {

        private readonly ILogger<BarMatchTeamDeathDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public BarMatchTeamDeathDb(ILogger<BarMatchTeamDeathDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        /// <summary>
        ///     get all <see cref="BarMatchTeamDeath"/>s of a specific game
        /// </summary>
        /// <param name="gameID"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<List<BarMatchTeamDeath>> GetByGameID(string gameID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<BarMatchTeamDeath>(
                "SELECT * FROM bar_match_team_death WHERE game_id = @GameID",
                new { GameID = gameID },
                cancel
            );
        }

        /// <summary>
        ///     insert a new <see cref="BarMatchTeamDeath"/> to the DB
        /// </summary>
        /// <param name="death"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task Insert(BarMatchTeamDeath death, CancellationToken cancel) {
            if (string.IsNullOrEmpty(death.GameID)) {
                throw new Exception($"missing {nameof(BarMatchTeamDeath.GameID)} from the {nameof(BarMatchTeamDeath)}");
            }

            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO bar_match_team_death (
                    game_id, team_id, reason, game_time
                ) VALUES (
                    @GameID, @TeamID, @Reason, @GameTime
                );
            ");

            cmd.AddParameter("GameID", death.GameID);
            cmd.AddParameter("TeamID", death.TeamID);
            cmd.AddParameter("Reason", death.Reason);
            cmd.AddParameter("GameTime", death.GameTime);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

        /// <summary>
        ///     remove all <see cref="BarMatchTeamDeath"/>s from the DB for a specific game
        /// </summary>
        /// <param name="gameID">ID of the game to remove the team deaths of</param>
        /// <returns></returns>
        public async Task DeleteByGameID(string gameID) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                DELETE FROM bar_match_team_death
                    WHERE game_id = @GameID;
            ");

            cmd.AddParameter("GameID", gameID);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
