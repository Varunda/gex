using Dapper;
using gex.Code.ExtensionMethods;
using gex.Models.Event;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace gex.Services.Db {

    public class GameEventWindUpdateDb : BaseGameEventDb<GameEventWindUpdate> {

        public GameEventWindUpdateDb(ILoggerFactory loggerFactory, IDbHelper dbHelper)
            : base ("game_event_wind_update", "wind_update", loggerFactory, dbHelper) { }

        protected override void SetupInsert(GameEventWindUpdate ev, NpgsqlCommand cmd) {
            cmd.CommandText = @"
                INSERT INTO game_event_wind_update (
                    game_id, frame, value
                ) VALUES (
                    @GameID, @Frame, @Value
                );
            ";

            cmd.AddParameter("GameID", ev.GameID);
            cmd.AddParameter("Frame", ev.Frame);
            cmd.AddParameter("Value", ev.Value);
        }

    }
}
