using gex.Common.Services.Db;
using Npgsql;
using System.Data.Common;
using gex.Common.Code.ExtensionMethods;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch20AddDurationFrameCountToBarMatch : IDbPatch {
        public int MinVersion => 20;
        public string Name => "add duration_frame_count to bar_match";

        public async Task Execute(IDbHelper helper) {
            using DbConnection conn = helper.Connection(Dbs.MAIN);
            using DbCommand cmd = await helper.Command(conn, @"
                ALTER TABLE bar_match ADD COLUMN IF NOT EXISTS duration_frame_count bigint NOT NULL DEFAULT 0;
			");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
