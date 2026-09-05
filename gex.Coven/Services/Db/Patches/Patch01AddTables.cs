using gex.Common.Services.Db;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.Services.Db.Patches;

[Patch]
public class Patch01AddTables : IDbPatch {
    public int MinVersion => 1;
    public string Name => "add tables";

    public async Task Execute(IDbHelper helper) {
        using DbConnection conn = helper.Connection(SqLiteDb.WRITE);
        using DbCommand cmd = await helper.Command(conn, @"
            CREATE TABLE IF NOT EXISTS bar_match_hash (
                id text NOT NULL PRIMARY KEY,
                filename text NOT NULL,
                hash text NOT NULL
            ) STRICT;

            CREATE INDEX idx_bar_match_hash_hash ON bar_match_hash (hash);

            CREATE TABLE IF NOT EXISTS bar_match (
                id text NOT NULL PRIMARY KEY,
                engine text NOT NULL,
                game_version text NOT NULL,
                file_name text NOT NULL,
                start_time integer NOT NULL,
                map text NOT NULL,
                duration_ms integer NOT NULL,
                host_settings text NOT NULL, -- json
                game_settings text NOT NULL, -- json
                map_settings text NOT NULL, -- json
                spads_settings text NOT NULL, -- json
                restrictions text NOT NULL, -- json
                map_name text NOT NULL,
                gamemode integer NOT NULL,
                duration_frame_count integer NOT NULL,
                player_count integer NOT NULL,
                wrong_skill_values integer NOT NULL, -- boolean
                start_offset real NOT NULL,
                average_os real NOT NULL,
                min_os real NOT NULL,
                max_os real NOT NULL,
                start_spot_version integer
            ) STRICT;
        ");

        await cmd.ExecuteNonQueryAsync();
    }

}
