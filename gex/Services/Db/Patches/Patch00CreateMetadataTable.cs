using gex.Common.Services.Db;
using Npgsql;
using System.Data.Common;
using gex.Common.Code.ExtensionMethods;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch00CreateMetadataTable : IDbPatch {

        public int MinVersion => 0;
        public string Name => $"CreateMetadataTable";

        public async Task Execute(IDbHelper helper) {
            using DbConnection conn = helper.Connection();
            using DbCommand cmd = await helper.Command(conn, @"
                CREATE TABLE IF NOT EXISTS metadata (
                    name varchar NOT NULL PRIMARY KEY,
                    value varchar NOT NULL
                );
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
