using gex.Code.ExtensionMethods;
using gex.Models.Event;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace gex.Services.Db.Event {

    public class GameEventUnitDamageDb : BaseGameEventDb<GameEventUnitDamage> {

        public GameEventUnitDamageDb(ILoggerFactory loggerFactory, IDbHelper dbHelper)
            : base("game_event_unit_damage", "unit_damage", loggerFactory, dbHelper) { }

        protected override void SetupInsert(GameEventUnitDamage ev, NpgsqlCommand cmd) {
            cmd.CommandText = @"
                INSERT INTO game_event_unit_damage (
                    game_id, frame, 
                    unit_id, definition_id, team_id, damage_dealt, damage_taken
                ) VALUES (
                    @GameID, @Frame,
                    @UnitID, @DefinitionID, @TeamID, @DamageDealt, @DamageTaken
                );
            ";

            cmd.AddParameter("GameID", ev.GameID);
            cmd.AddParameter("Frame", ev.Frame);
            cmd.AddParameter("UnitID", ev.UnitID);
            cmd.AddParameter("DefinitionID", ev.DefinitionID);
            cmd.AddParameter("TeamID", ev.TeamID);
            cmd.AddParameter("DamageDealt", ev.DamageDealt);
            cmd.AddParameter("DamageTaken", ev.DamageTaken);
        }

    }
}
