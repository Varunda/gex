using Dapper.ColumnMapper;
using gex.Code;

namespace gex.Models.Event {

    [DapperColumnsMapped]
    public class GameEvent {

        [ColumnMapping("game_id")]
        public string GameID { get; set; } = "";

        public string Action { get; set; } = GameActionType.UNKNOWN;

        [ColumnMapping("frame")]
        public long Frame { get; set; }

    }

    public sealed class GameActionType {

        public const string UNKNOWN = "unknown";

        public const string INIT = "init";

        public const string START = "start";

        public const string WIND_UPDATE = "wind_update";

        public const string TEAM_DIED = "team_died";

        public const string UNIT_DEF = "unit_def";

        public const string TEAM_STATS = "team_stats";

        public const string UNIT_CREATED = "unit_created";

        public const string UNIT_KILLED = "unit_killed";

        public const string UNIT_GIVEN = "unit_given";

        public const string UNIT_TAKEN = "unit_taken";

        public const string EXTRA_STATS = "extra_stat_update";

        public const string FACTORY_UNIT_CREATE = "factory_unit_created";

        public const string COMMANDER_POSITION_UPDATE = "commander_position_update";

        public const string TRANSPORT_LOADED = "transport_loaded";

        public const string TRANSPORT_UNLOADED = "transport_unloaded";

        public const string UNIT_RESOURCES = "unit_resources";

        public const string UNIT_DAMAGE = "unit_damage";

        public const string END = "end";

        public const string SHUTDOWN = "shutdown";

    }

}
