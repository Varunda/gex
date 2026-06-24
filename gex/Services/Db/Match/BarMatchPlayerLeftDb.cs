using gex.Code.ExtensionMethods;
using gex.Models.Db;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.Match {

    public class BarMatchPlayerLeftDb {

        private readonly ILogger<BarMatchPlayerLeftDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public BarMatchPlayerLeftDb(ILogger<BarMatchPlayerLeftDb> logger,
            IDbHelper dbHelper) {
            
            _Logger = logger;
            _DbHelper = dbHelper;
        }

        public async Task Insert(BarMatchPlayerLeft left, CancellationToken cancel) {
            if (string.IsNullOrEmpty(left.GameID)) {
                throw new ArgumentException($"missing GameID of player left");
            }

            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO bar_match_player_left (
                    game_id, player_id, reason, game_time, index
                ) VALUES (
                    @GameID, @PlayerID, @Reason, @GameTime, @Index
                );
            ", cancel);

            cmd.AddParameter("GameID", left.GameID);
            cmd.AddParameter("PlayerID", left.PlayerID);
            cmd.AddParameter("Reason", left.Reason);
            cmd.AddParameter("GameTime", left.GameTime);
            cmd.AddParameter("Index", left.Index);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

        public async Task<List<BarMatchPlayerLeft>> GetByGameID(string gameID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<BarMatchPlayerLeft>(
                "SELECT * FROM bar_match_player_left WHERE game_id = @GameID ORDER BY game_time ASC",
                new { GameID = gameID },
                cancellationToken: cancel
            );
        }

        public async Task DeleteByGameID(string gameID) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                DELETE FROM bar_match_player_left
                    WHERE game_id = @GameID;
            ");

            cmd.AddParameter("GameID", gameID);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
