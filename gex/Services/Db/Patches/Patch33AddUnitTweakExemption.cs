using gex.Common.Services.Db;
using Npgsql;
using System.Data.Common;
using gex.Common.Code.ExtensionMethods;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch33AddUnitTweakExemption : IDbPatch {
        public int MinVersion => 33;
        public string Name => "add unit_tweak_exemption";

        public async Task Execute(IDbHelper helper) {
            using DbConnection conn = helper.Connection(Dbs.MAIN);
            using DbCommand cmd = await helper.Command(conn, @"
				CREATE TABLE IF NOT EXISTS unit_tweak_exemption (
					unit_tweak varchar NOT NULL PRIMARY KEY,
					timestamp timestamptz NOT NULL
				);
			");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
