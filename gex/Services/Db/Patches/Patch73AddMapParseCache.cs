using gex.Common.Services.Db;
using System.Data.Common;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch73AddMapParseCache : IDbPatch {
        public int MinVersion => 73;
        public string Name => "add bar_map_parse_cache";

        public async Task Execute(IDbHelper helper) {
            using DbConnection conn = helper.Connection(Dbs.MAIN);
            using DbCommand cmd = await helper.Command(conn, @"
                CREATE TABLE IF NOT EXISTS bar_map_parse_cache (
                    map_filename VARCHAR not null PRIMARY KEY,
                    timestamp timestamptz NOT NULL DEFAULT (NOW() at time zone 'utc'),
                    
                    data jsonb NOT NULL
                );
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
