using gex.Common.Services.Db;
using System.Data.Common;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch71AddOpeningLabToMatchTeam : IDbPatch {
        public int MinVersion => 71;
        public string Name => "add opening_lab_unit_definition_name to bar_match_team";

        public async Task Execute(IDbHelper helper) {
            using DbConnection conn = helper.Connection(Dbs.MAIN);
            using DbCommand cmd = await helper.Command(conn, @"
                ALTER TABLE bar_match_team
                    ADD COLUMN IF NOT EXISTS opening_lab_unit_definition_name varchar NULL;
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
