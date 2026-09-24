using gex.Common.Services.Db;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.Services.Db.Patches {

    [Patch]
    public class Patch02AddOpeningLabToMatchTeam : IDbPatch {
        public int MinVersion => 2;
        public string Name => "add opening_lab_unit_definition_name to bar_match_team";

        public async Task Execute(IDbHelper helper) {
            using DbConnection conn = helper.Connection(SqLiteDb.WRITE);
            using DbCommand cmd = await helper.Command(conn, @"
                ALTER TABLE bar_match_team
                    ADD COLUMN opening_lab_unit_definition_name TEXT NULL;
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
