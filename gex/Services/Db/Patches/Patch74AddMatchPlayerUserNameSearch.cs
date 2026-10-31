using gex.Common.Services.Db;
using System.Data.Common;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch74AddMatchPlayerUserNameSearch : IDbPatch {
        public int MinVersion => 74;
        public string Name => "add an index to bar_match_player.user_name";

        public async Task Execute(IDbHelper helper) {
            using DbConnection conn = helper.Connection(Dbs.MAIN);
            using DbCommand cmd = await helper.Command(conn, @"
				CREATE INDEX IF NOT EXISTS idx_bar_match_player_user_name ON bar_match_player (lower(user_name));
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
