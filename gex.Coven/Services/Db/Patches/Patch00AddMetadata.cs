using gex.Common.Services.Db;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.Services.Db.Patches;

[Patch]
public class Patch00AddMetadata : IDbPatch {
    public int MinVersion => 0;
    public string Name => "add metadata table";

    public async Task Execute(IDbHelper helper) {
        using DbConnection conn = helper.Connection(SqLiteDb.WRITE);
        using DbCommand cmd = await helper.Command(conn, @"
            CREATE TABLE IF NOT EXISTS metadata (
                name text NOT NULL PRIMARY KEY,
                value text NOT NULL
            ) STRICT;
        ");

        await cmd.ExecuteNonQueryAsync();
    }

}
