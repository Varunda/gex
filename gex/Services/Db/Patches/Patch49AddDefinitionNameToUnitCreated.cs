using gex.Common.Services.Db;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Npgsql;
using System.Data.Common;
using gex.Common.Code.ExtensionMethods;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch49AddDefinitionNameToUnitCreated : IDbPatch {
        public int MinVersion => 49;
        public string Name => "add definition_name to game_event_unit_created";

        public async Task Execute(IDbHelper helper) {
            using DbConnection conn = helper.Connection(Dbs.MAIN);
            using DbCommand cmd = await helper.Command(conn, @"
                ALTER TABLE game_event_unit_created
                    ADD COLUMN IF NOT EXISTS definition_name varchar NOT NULL DEFAULT '';
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
