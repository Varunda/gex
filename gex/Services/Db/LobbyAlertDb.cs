using Dapper;
using gex.Code.ExtensionMethods;
using gex.Models.Db;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db {

    public class LobbyAlertDb {

        private readonly ILogger<LobbyAlertDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public LobbyAlertDb(ILogger<LobbyAlertDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        public async Task<LobbyAlert?> GetByID(long alertID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QuerySingleAsync<LobbyAlert>(@"
                SELECT * FROM lobby_alert WHERE id = @AlertID;
            ", new { AlertID = alertID }, cancel);
        }

        public async Task<List<LobbyAlert>> GetByChannelID(ulong channelID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<LobbyAlert>(@"
                SELECT * FROM lobby_alert WHERE channel_id = @ChannelID;
            ", new { ChannelID = unchecked((long)channelID) }, cancel);
        }

        public async Task<List<LobbyAlert>> GetAll(CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<LobbyAlert>(@"
                SELECT * FROM lobby_alert;
            ", cancel);
        }

        public async Task<long> Insert(LobbyAlert alert, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO lobby_alert (
                    guild_id,
                    channel_id,
                    role_id,
                    created_by_id,
                    timestamp,
                    time_between_alerts_seconds,
        
                    minimum_os, maximum_os,
                    minimum_average_os, maximum_average_os,
                    minimum_player_count, maximum_player_count,
                    gamemode
                ) VALUES (
                    @GuildID,
                    @ChannelID,
                    @RoleID,
                    @CreatedByID,
                    @Timestamp,
                    @TimeBetweenAlertsSeconds,
        
                    @MinimumOS, @MaximumOS,
                    @MinimumAverageOS, @MaximumAverageOS,
                    @MinimumPlayerCount, @MaximumPlayerCount,
                    @Gamemode
                ) RETURNING id;
            ", cancel);

            cmd.AddParameter("GuildID", alert.GuildID);
            cmd.AddParameter("ChannelID", alert.ChannelID);
            cmd.AddParameter("RoleID", alert.RoleID);
            cmd.AddParameter("CreatedByID", alert.CreatedByID);
            cmd.AddParameter("Timestamp", alert.Timestamp);
            cmd.AddParameter("TimeBetweenAlertsSeconds", alert.TimeBetweenAlertsSeconds);
            cmd.AddParameter("MinimumOS", alert.MinimumOS);
            cmd.AddParameter("MaximumOS", alert.MaximumOS);
            cmd.AddParameter("MinimumAverageOS", alert.MinimumAverageOS);
            cmd.AddParameter("MaximumAverageOS", alert.MaximumAverageOS);
            cmd.AddParameter("MinimumPlayerCount", alert.MinimumPlayerCount);
            cmd.AddParameter("MaximumPlayerCount", alert.MaximumPlayerCount);
            cmd.AddParameter("Gamemode", alert.Gamemode);
            await cmd.PrepareAsync(cancel);

            return await cmd.ExecuteInt64(cancel);
        }

        public async Task DeleteByID(long ID, CancellationToken cancel) {
            NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                DELETE FROM lobby_alert
                    WHERE id = @ID;
            ", cancel);

            cmd.AddParameter("ID", ID);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

    }
}
