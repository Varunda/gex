using Dapper;
using gex.Code.ExtensionMethods;
using gex.Models.Event;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace gex.Services.Db.Event {

    public class GameEventExtraStatsDb : BaseGameEventDb<GameEventExtraStatUpdate> {

        public GameEventExtraStatsDb(ILoggerFactory loggerFactory, IDbHelper dbHelper)
            : base("game_event_extra_stats", "extra_stat_update", loggerFactory, dbHelper) { }

        protected override void SetupInsert(GameEventExtraStatUpdate ev, NpgsqlCommand cmd) {
            cmd.CommandText = @"
                INSERT INTO game_event_extra_stats (
                    game_id, frame, team_id,
                    total_value, army_value, defense_value, util_value, eco_value, other_value,
                    build_power_available, build_power_used, metal_current, energy_current
                ) VALUES (
                    @GameID, @Frame, @TeamID,
                    @TotalValue, @ArmyValue, @DefenseValue, @UtilValue, @EcoValue, @OtherValue,
                    @BuildPowerAvailable, @BuildPowerUsed, @MetalCurrent, @EnergyCurrent
                );
            ";

            cmd.AddParameter("GameID", ev.GameID);
            cmd.AddParameter("Frame", ev.Frame);
            cmd.AddParameter("TeamID", ev.TeamID);

            cmd.AddParameter("TotalValue", ev.TotalValue);
            cmd.AddParameter("ArmyValue", ev.ArmyValue);
            cmd.AddParameter("DefenseValue", ev.DefenseValue);
            cmd.AddParameter("UtilValue", ev.UtilValue);
            cmd.AddParameter("EcoValue", ev.EcoValue);
            cmd.AddParameter("OtherValue", ev.OtherValue);
            cmd.AddParameter("MetalCurrent", ev.MetalCurrent);
            cmd.AddParameter("EnergyCurrent", ev.EnergyCurrent);

            cmd.AddParameter("BuildPowerAvailable", ev.BuildPowerAvailable);
            cmd.AddParameter("BuildPowerUsed", ev.BuildPowerUsed);
        }

    }
}
