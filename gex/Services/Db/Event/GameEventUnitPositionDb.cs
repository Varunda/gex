using gex.Code.ExtensionMethods;
using gex.Models.Event;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace gex.Services.Db.Event {

	public class GameEventUnitPositionDb : BaseGameEventDb<GameEventUnitPosition> {

		public GameEventUnitPositionDb(ILoggerFactory loggerFactory, IDbHelper dbHelper)
			: base("game_event_unit_position", "unit_position", loggerFactory, dbHelper) { }

		protected override void SetupInsert(GameEventUnitPosition ev, NpgsqlCommand cmd) {
			cmd.CommandText = @"
				INSERT INTO game_event_unit_position (
					game_id, frame, unit_id, team_id, x, y, z
				) VALUES (
					@GameID, @Frame, @UnitID, @TeamID, @X, @Y, @Z
				);
			";

			cmd.AddParameter("GameID", ev.GameID);
			cmd.AddParameter("Frame", ev.Frame);
			cmd.AddParameter("UnitID", ev.UnitID);
			cmd.AddParameter("TeamID", ev.TeamID);
			cmd.AddParameter("X", ev.X);
			cmd.AddParameter("Y", ev.Y);
			cmd.AddParameter("Z", ev.Z);
		}

	}
}
