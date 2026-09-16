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

            CREATE INDEX IF NOT EXISTS idx_bar_match_hash_hash ON bar_match_hash (hash);

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

            CREATE TABLE IF NOT EXISTS bar_match_ally_team (
                game_id             text NOT NULL,
                ally_team_id        integer NOT NULL,
                player_count        integer NOT NULL,
                won                 integer NOT NULL, -- boolean
                start_box_top       real NOT NULL,
                start_box_bottom    real NOT NULL,
                start_box_left      real NOT NULL,
                start_box_right     real NOT NULL,
                average_skill       real NOT NULL,

                PRIMARY KEY (game_id, ally_team_id)
            ) STRICT;

            CREATE TABLE IF NOT EXISTS bar_match_player (
                game_id             text NOT NULL,
                player_id           integer NOT NULL,
                user_id             integer NOT NULL,
                user_name           text NOT NULL,
                team_id             integer NOT NULL,
                ally_team_id        integer NOT NULL,
                skill               real NOT NULL,
                skill_uncertainty   real NOT NULL,

                PRIMARY KEY (game_id, player_id)
            ) STRICT;

            CREATE TABLE IF NOT EXISTS bar_match_team (
                game_id                 text NOT NULL,
                team_id                 integer NOT NULL,
                ally_team_id            integer NOT NULL,
                team_leader_id          integer NOT NULL,
                faction                 text NOT NULL,
                starting_position_x     real NOT NULL,
                starting_position_y     real NOT NULL,
                starting_position_z     real NOT NULL,
                color                   integer NOT NULL,
                handicap                real NOT NULL,
                start_spot              text NULL,
                start_spot_label        text NULL,

                PRIMARY KEY (game_id, team_id)
            ) STRICT;

            CREATE TABLE IF NOT EXISTS bar_match_ai_player (
                game_id     text NOT NULL,
                ai_id       integer NOT NULL,
                team_id     integer NOT NULL,
                name        text NOT NULL,
                
                PRIMARY KEY (game_id, ai_id)
            ) STRICT;

            CREATE TABLE IF NOT EXISTS bar_map (
                id                  integer NOT NULL,
                name                text NOT NULL,
                filename            text NOT NULL,
                description         text NOT NULL,
                tidal_strength      real NOT NULL,
                max_metal           real NOT NULL,
                extractor_radius    real NOT NULL,
                minimum_wind        real NOT NULL,
                maximum_wind        real NOT NULL,
                width               real NOT NULL,
                height              real NOT NULL,
                author              text NOT NULL,
                timestamp           integer NOT NULL,
                symmetry_axis       integer NULL,

                PRIMARY KEY (id)
            ) STRICT;

            CREATE TABLE IF NOT EXISTS bar_match_ignored_files (
                filename    text NOT NULL,
                reason      text NOT NULL,

                PRIMARY KEY (filename)
            ) STRICT;
        ");

        await cmd.ExecuteNonQueryAsync();
    }

}
