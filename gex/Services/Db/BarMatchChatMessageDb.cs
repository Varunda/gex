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

    public class BarMatchChatMessageDb {

        private readonly ILogger<BarMatchChatMessageDb> _Logger;
        private readonly IDbHelper _DbHelper;
        private readonly IDataReader<BarMatchChatMessage> _Reader;

        public BarMatchChatMessageDb(ILogger<BarMatchChatMessageDb> logger,
            IDbHelper dbHelper, IDataReader<BarMatchChatMessage> reader) {

            _Logger = logger;
            _DbHelper = dbHelper;
            _Reader = reader;
        }

        public async Task Insert(BarMatchChatMessage msg) {
            if (string.IsNullOrEmpty(msg.GameID)) {
                throw new ArgumentException($"missing GameID of chat message");
            }

            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO bar_match_chat_message (
                    game_id, game_timestamp, to_id, from_id, message
                ) VALUES (
                    @GameID, @GameTimestamp, @ToId, @FromId, @Message
                );
            ");

            cmd.AddParameter("GameID", msg.GameID);
            cmd.AddParameter("GameTimestamp", msg.GameTimestamp);
            cmd.AddParameter("ToId", msg.ToId);
            cmd.AddParameter("FromId", msg.FromId);
            cmd.AddParameter("Message", msg.Message);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

        public async Task<List<BarMatchChatMessage>> GetByGameID(string gameID) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return (await conn.QueryAsync<BarMatchChatMessage>(
                "SELECT * FROM bar_match_chat_message WHERE game_id = @GameID ORDER BY game_timestamp ASC",
                new { GameID = gameID }
            )).ToList();
        }

        public async Task DeleteByGameID(string gameID) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                DELETE FROM bar_match_chat_message
                    WHERE game_id = @GameID;
            ");

            cmd.AddParameter("GameID", gameID);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
