using gex.Code.ExtensionMethods;
using gex.Models.Db;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db {

    public class MatchProcessingWebhookDb {

        private readonly ILogger<MatchProcessingWebhookDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public MatchProcessingWebhookDb(ILogger<MatchProcessingWebhookDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        public async Task<List<MatchProcessingWebhook>> GetAll(CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<MatchProcessingWebhook>(
                "SELECT * FROM match_processing_webhook",
                cancel
            );
        }

        public async Task<MatchProcessingWebhook?> Get(string url, string type, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QuerySingleAsync<MatchProcessingWebhook>(
                "SELECT * FROM match_processing_webhook WHERE url = @Url and type = LOWER(@Type)",
                new {
                    Url = url,
                    Type = type
                },
                cancel
            );
        }

        public async Task Upsert(MatchProcessingWebhook webhook, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO match_processing_webhook (
                    url, type, shared_secret, include_events, timestamp, ip
                ) VALUES (
                    @Url, @Type, @SharedSecret, @IncludeEvents, NOW() at time zone 'utc', @IP
                ) ON CONFLICT (url, type) DO UPDATE SET
                    include_events = @IncludeEvents,
                    timestamp = NOW() at time zone 'utc'
            ");

            cmd.AddParameter("Url", webhook.Url);
            cmd.AddParameter("Type", webhook.Type.ToLower());
            cmd.AddParameter("SharedSecret", webhook.SharedSecret);
            cmd.AddParameter("IncludeEvents", webhook.IncludeEvents);
            cmd.AddParameter("IP", webhook.IP);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

        public async Task Delete(MatchProcessingWebhook webhook, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                DELETE FROM match_processing_webhook
                    WHERE url = @Url
                        AND type = @Type
                        AND shared_secret = @SharedSecret;
            ");

            cmd.AddParameter("Url", webhook.Url);
            cmd.AddParameter("Type", webhook.Type.ToLower());
            cmd.AddParameter("SharedSecret", webhook.SharedSecret);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

    }
}
