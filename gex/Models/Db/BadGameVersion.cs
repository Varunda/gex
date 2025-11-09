using Dapper.ColumnMapper;
using gex.Code;

namespace gex.Models.Db {

    /// <summary>
    ///     represents a game version that cannot be ran in headless for whatever reason
    /// </summary>
    [DapperColumnsMapped]
    public class BadGameVersion {

        [ColumnMapping("game_version")]
        public string GameVersion { get; set; } = "";

    }
}
