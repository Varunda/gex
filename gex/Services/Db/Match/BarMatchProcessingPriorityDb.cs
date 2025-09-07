using gex.Code.ExtensionMethods;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.Match {

    public class BarMatchProcessingPriorityDb {

        private readonly ILogger<BarMatchProcessingPriorityDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public BarMatchProcessingPriorityDb(ILogger<BarMatchProcessingPriorityDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        public async Task Upsert(ulong discordID, string gameID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO bar_match_processing_priority (
                    discord_id, game_id, timestamp
                ) VALUES (
                    @DiscordID, @GameID, now() at time zone 'utc'
                ) ON CONFLICT (discord_id) DO
                    UPDATE SET game_id = @GameID,
                        timestamp = now() at time zone 'utc';
            ", cancel);

            cmd.AddParameter("DiscordID", discordID);
            cmd.AddParameter("GameID", gameID);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

        public async Task<string?> GetByDiscordID(ulong discordID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QuerySingleAsync<string>(@"
                SELECT * FROM bar_match_processing_priority WHERE discord_id = @DiscordID;
            ", new { DiscordID = discordID }, cancel);
        }

        public async Task<List<ulong>> GetByGameID(string gameID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);

            List<long> ret = await conn.QueryListAsync<long>(@"
                SELECT * FROM bar_match_processing_priority WHERE game_id = @GameID;
            ", new { GameID = gameID }, cancel);

            return ret.Select(iter => unchecked((ulong)iter)).ToList();
        }

    }
}
