using gex.Code.ExtensionMethods;
using gex.Models.Event;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Runtime.InteropServices.ObjectiveC;
using System.Threading.Tasks;

namespace gex.Services.Db.Event {

    public class GameEventCommanderPositionUpdateDb : BaseGameEventDb<GameEventCommanderPositionUpdate> {

        public GameEventCommanderPositionUpdateDb(ILoggerFactory loggerFactory, IDbHelper dbHelper)
            : base("game_event_commander_position_update", "commander_position_update", loggerFactory, dbHelper) { }

        protected override void SetupInsert(GameEventCommanderPositionUpdate ev, NpgsqlCommand cmd) {
            cmd.CommandText = @"
                INSERT INTO game_event_commander_position_update (
                    game_id, frame, unit_id, unit_x, unit_y, unit_z
                ) VALUES (
                    @GameID, @Frame, @UnitID, @UnitX, @UnitY, @UnitZ
                );
            ";

            cmd.AddParameter("GameID", ev.GameID);
            cmd.AddParameter("Frame", ev.Frame);
            cmd.AddParameter("UnitID", ev.UnitID);
            cmd.AddParameter("UnitX", ev.UnitX);
            cmd.AddParameter("UnitY", ev.UnitY);
            cmd.AddParameter("UnitZ", ev.UnitZ);
        }

    }
}
