using Dapper;
using gex.Code.ExtensionMethods;
using gex.Models.Event;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace gex.Services.Db {

    public class GameEventArmyValueUpdateDb : BaseGameEventDb<GameEventArmyValueUpdate> {

        public GameEventArmyValueUpdateDb(ILoggerFactory loggerFactory, IDbHelper dbHelper)
            : base("game_event_army_value_update", "army_value_update", loggerFactory, dbHelper) { }

        protected override void SetupInsert(GameEventArmyValueUpdate ev, NpgsqlCommand cmd) {
            cmd.CommandText = @"
                INSERT INTO game_event_army_value_update (
                    game_id, frame, team_id, value
                ) VALUES (
                    @GameID, @Frame, @TeamID, @Value
                );
            ";

            cmd.AddParameter("GameID", ev.GameID);
            cmd.AddParameter("Frame", ev.Frame);
            cmd.AddParameter("TeamID", ev.TeamID);
            cmd.AddParameter("Value", ev.Value);
        }

    }
}
