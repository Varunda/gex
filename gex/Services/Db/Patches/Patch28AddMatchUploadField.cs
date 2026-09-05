using gex.Common.Services.Db;
using Npgsql;
using System.Data.Common;
using gex.Common.Code.ExtensionMethods;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch28AddMatchUploadField : IDbPatch {
        public int MinVersion => 28;
        public string Name => "add uploaded_by to bar_match";

        public async Task Execute(IDbHelper helper) {
            using DbConnection conn = helper.Connection(Dbs.MAIN);
            using DbCommand cmd = await helper.Command(conn, @"
				ALTER TABLE bar_match
					ADD COLUMN IF NOT EXISTS uploaded_by bigint NULL;
			");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }
    }
}
