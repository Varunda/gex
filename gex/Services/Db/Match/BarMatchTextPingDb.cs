using gex.Code.ExtensionMethods;
using gex.Models.Db;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.Match {

    public class BarMatchTextPingDb {

        private readonly ILogger<BarMatchTextPingDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public BarMatchTextPingDb(ILogger<BarMatchTextPingDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        public async Task Insert(BarMatchMapDrawPoint point, CancellationToken cancel) {
            if (string.IsNullOrEmpty(point.GameID)) {
                throw new ArgumentException($"missing GameID of bar match map draw point");
            }

            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO bar_match_map_draw_point (
                    game_id, player_id, index, game_time, x, z, label, from_lua
                ) VALUES (
                    @GameID, @PlayerID, @Index, @GameTime, @X, @Z, @Label, @FromLua
                );
            ", cancel);

            cmd.AddParameter("GameID", point.GameID);
            cmd.AddParameter("PlayerID", point.PlayerID);
            cmd.AddParameter("Index", point.Index);
            cmd.AddParameter("GameTime", point.GameTime);
            cmd.AddParameter("X", point.X);
            cmd.AddParameter("Z", point.Z);
            cmd.AddParameter("Label", point.Label);
            cmd.AddParameter("FromLua", point.FromLua);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

        public async Task<List<BarMatchMapDrawPoint>> GetByGameID(string gameID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<BarMatchMapDrawPoint>(
                @"SELECT *, 'point' ""action"" FROM bar_match_map_draw_point WHERE game_id = @GameID ORDER BY game_time ASC",
                new { GameID = gameID },
                cancellationToken: cancel
            );
        }

        public async Task DeleteByGameID(string gameID) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                DELETE FROM bar_match_map_draw_point
                    WHERE game_id = @GameID;
            ");

            cmd.AddParameter("GameID", gameID);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
