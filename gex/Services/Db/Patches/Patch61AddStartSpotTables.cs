using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch61AddStartSpotTables : IDbPatch {
        public int MinVersion => 61;
        public string Name => "add start spot data";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                ALTER TABLE bar_map
                    DROP COLUMN IF EXISTS start_position_data;

                ALTER TABLE bar_match
                    ADD COLUMN IF NOT EXISTS start_spot_version int NULL;

                ALTER TABLE bar_match_player
                    ADD COLUMN IF NOT EXISTS start_spot varchar NULL;
                
                ALTER TABLE bar_match_player
                    ADD COLUMN IF NOT EXISTS start_spot_label varchar NULL;

                CREATE TABLE IF NOT EXISTS start_spot_data (
                    map_filename varchar NOT NULL,
                    version int NOT NULL,

                    raw jsonb NOT NULL,

                    timestamp timestamptz NOT NULL,
                    min_timestamp timestamptz NOT NULL,
                    max_timestamp timestamptz NULL,

                    PRIMARY KEY (map_filename, version)
                );

                CREATE TABLE IF NOT EXISTS start_spot_position (
                    map_filename varchar NOT NULL,
                    version int NOT NULL,

                    name varchar NOT NULL,

                    x float NOT NULL,
                    y float NOT NULL,
            
                    PRIMARY KEY (map_filename, version, name)
                );

                CREATE TABLE IF NOT EXISTS start_spot_configuration (
                    map_filename varchar NOT NULL,
                    version int NOT NULL,

                    players_per_team int NOT NULL,
                    team_count int NOT NULL,

                    PRIMARY KEY (map_filename, version, players_per_team, team_count)
                );

                CREATE TABLE IF NOT EXISTS start_spot_side (
                    map_filename varchar NOT NULL,
                    version int NOT NULL,

                    index int NOT NULL,

                    players_per_team int NOT NULL,
                    team_count int NOT NULL,

                    PRIMARY KEY (map_filename, version, index)
                );
            
                CREATE TABLE IF NOT EXISTS start_spot_side_start (
                    map_filename varchar NOT NULL,
                    version int NOT NULL,

                    spawn_point varchar NOT NULL,

                    side_index int NOT NULL,
                    base_center varchar NULL,
                    role varchar NOT NULL,

                    PRIMARY KEY (map_filename, version, side_index, spawn_point)
                );

                CREATE TABLE IF NOT EXISTS start_spot_side_start_role_override (
                    map_filename varchar NOT NULL,
                    version int NOT NULL,

                    position varchar NOT NULL,

                    role varchar NOT NULL,
                    max_radius numeric NULL,
                    timestamp timestamptz NOT NULL DEFAULT (NOW() at time zone 'utc'),

                    PRIMARY KEY (map_filename, version, position)
                );
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
