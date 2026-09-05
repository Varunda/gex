using gex.Common.Services.Db;
using Npgsql;
using System.Data.Common;
using gex.Common.Code.ExtensionMethods;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch06AddSizeToUnitDef : IDbPatch {

        public int MinVersion => 6;

        public string Name => "add size to unit_def_set_entry";

        public async Task Execute(IDbHelper helper) {
            using DbConnection conn = helper.Connection(Dbs.MAIN);
            using DbCommand cmd = await helper.Command(conn, @"
                alter table unit_def_set_entry ADD COLUMN IF NOT EXISTS size_x double precision NOT NULL default 0;
                alter table unit_def_set_entry ADD COLUMN IF NOT EXISTS size_z double precision NOT NULL default 0;
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
