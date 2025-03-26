﻿using Dapper;
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
                    game_id, frame, team_id, army_value, build_power_available, build_power_used
                ) VALUES (
                    @GameID, @Frame, @TeamID, @ArmyValue, @BuildPowerAvailable, @BuildPowerUsed
                );
            ";

            cmd.AddParameter("GameID", ev.GameID);
            cmd.AddParameter("Frame", ev.Frame);
            cmd.AddParameter("TeamID", ev.TeamID);
            cmd.AddParameter("ArmyValue", ev.ArmyValue);
            cmd.AddParameter("BuildPowerAvailable", ev.BuildPowerAvailable);
            cmd.AddParameter("BuildPowerUsed", ev.BuildPowerUsed);
        }

    }
}
