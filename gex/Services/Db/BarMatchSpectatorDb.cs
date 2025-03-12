using Dapper;
using gex.Code.ExtensionMethods;
using gex.Models.Db;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace gex.Services.Db {

    public class BarMatchSpectatorDb {

        private readonly ILogger<BarMatchSpectatorDb> _Logger;
        private readonly IDbHelper _DbHelper;
        private readonly IDataReader<BarMatchSpectator> _Reader;

        public BarMatchSpectatorDb(ILogger<BarMatchSpectatorDb> logger,
            IDbHelper dbHelper, IDataReader<BarMatchSpectator> reader) {

            _Logger = logger;
            _DbHelper = dbHelper;
            _Reader = reader;
        }

        public async Task Insert(BarMatchSpectator spec) {
            if (string.IsNullOrEmpty(spec.GameID)) {
                throw new ArgumentException($"missing GameID from spectator");
            }

            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO bar_match_spectator (
                    game_id, user_id, user_name
                ) VALUES (
                    @GameID, @UserID, @Username
                );
            ");

            cmd.AddParameter("GameID", spec.GameID);
            cmd.AddParameter("UserID", spec.UserID);
            cmd.AddParameter("Username", spec.Name);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

        public async Task<List<BarMatchSpectator>> GetByGameID(string gameID) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return (await conn.QueryAsync<BarMatchSpectator>(
                "SELECT * FROM bar_match_spectator WHERE game_id = @GameID",
                new { GameID = gameID }
            )).ToList();
        }

        public async Task DeleteByGameID(string gameID) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                DELETE FROM bar_match_spectator
                    WHERE game_id = @GameID;
            ");

            cmd.AddParameter("GameID", gameID);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
