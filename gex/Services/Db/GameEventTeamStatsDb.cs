using Dapper;
using gex.Code.ExtensionMethods;
using gex.Models.Event;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace gex.Services.Db {

    public class GameEventTeamStatsDb {

        private readonly ILogger<GameEventTeamStatsDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public GameEventTeamStatsDb(ILogger<GameEventTeamStatsDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        public async Task Insert(GameEventTeamStats ev) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO game_event_team_stats (
                    game_id, frame, team_id,
                    metal_produced, metal_used, metal_excess, metal_sent, metal_received,
                    energy_produced, energy_used, energy_excess, energy_sent, energy_received,
                    damage_dealt, damage_received,
                    units_produced, units_killed, units_sent, units_captured, units_out_captured
                ) VALUES (
                    @GameID, @Frame, @TeamID,
                    @MetalProduced, @MetalUsed, @MetalExcess, @MetalSent, @MetalReceived,
                    @EnergyProduced, @EnergyUsed, @EnergyExcess, @EnergySent, @EnergyReceived,
                    @DamageDealt, @DamageReceived,
                    @UnitsProduced, @UnitsKilled, @UnitsSent, @UnitsCaptured, @UnitsOutCaptured
                );
            ");

            cmd.AddParameter("GameID", ev.GameID);
            cmd.AddParameter("Frame", ev.Frame);
            cmd.AddParameter("TeamID", ev.TeamID);
            cmd.AddParameter("MetalProduced", ev.MetalProduced);
            cmd.AddParameter("MetalUsed", ev.Frame);
            cmd.AddParameter("MetalExcess", ev.MetalExcess);
            cmd.AddParameter("MetalSent", ev.MetalSent);
            cmd.AddParameter("MetalReceived", ev.MetalReceived);
            cmd.AddParameter("EnergyProduced", ev.EnergyProduced);
            cmd.AddParameter("EnergyUsed", ev.Frame);
            cmd.AddParameter("EnergyExcess", ev.EnergyExcess);
            cmd.AddParameter("EnergySent", ev.EnergySent);
            cmd.AddParameter("EnergyReceived", ev.EnergyReceived);
            cmd.AddParameter("DamageDealt", ev.DamageDealt);
            cmd.AddParameter("DamageReceived", ev.DamageReceived);
            cmd.AddParameter("UnitsProduced", ev.UnitsProduced);
            cmd.AddParameter("UnitsKilled", ev.UnitsKilled);
            cmd.AddParameter("UnitsSent", ev.UnitsSent);
            cmd.AddParameter("UnitsCaptured", ev.UnitsCaptured);
            cmd.AddParameter("UnitsOutCaptured", ev.UnitsOutCaptured);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

        public async Task<List<GameEventTeamStats>> GetByGameID(string gameID) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return (await conn.QueryAsync<GameEventTeamStats>(
                "SELECT 'team_stats' \"Action\", * FROM game_event_team_stats WHERE game_id = @GameID",
                new { GameID = gameID })
            ).ToList();
        }

        public async Task DeleteByGameID(string gameID) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                DELETE FROM game_event_team_stats
                    WHERE game_id = @GameID;
            ");

            cmd.AddParameter("GameID", gameID);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
