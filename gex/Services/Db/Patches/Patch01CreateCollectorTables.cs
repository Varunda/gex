using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch01CreateCollectorTables : IDbPatch {

        public int MinVersion => 1;
        public string Name => "create account tables";

        public async Task Execute(IDbHelper helper) {

            NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
				CREATE TABLE IF NOT EXISTS app_account (
                    id bigint NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
                    name varchar NOT NULL,
                    discord_id bigint NOT NULL,
                    timestamp timestamptz NOT NULL,
					deleted_on timestamptz NULL,
                    deleted_by bigint NULL
                );

                CREATE TABLE IF NOT EXISTS bar_replay (
                    id varchar NOT NULL PRIMARY KEY,
                    filename varchar NOT NULL
                );

                CREATE TABLE IF NOT EXISTS bar_match (
                    id varchar NOT NULL PRIMARY KEY,
					engine varchar NOT NULL,
					game_version varchar NOT NULL,
                    file_name varchar NOT NULL,
                    start_time timestamptz NOT NULL,
                    map varchar NOT NULL,
                    duration_ms bigint NOT NULL,
                    host_settings jsonb NOT NULL,
                    game_settings jsonb NOT NULL,
                    map_settings jsonb NOT NULL,
                    spads_settings jsonb NOT NULL,
                    restrictions jsonb NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_match_engine ON bar_match (engine);
                CREATE INDEX IF NOT EXISTS idx_match_game_version ON bar_match (game_version);
                CREATE INDEX IF NOT EXISTS idx_match_map ON bar_match (map);
                CREATE INDEX IF NOT EXISTS idx_match_start_time ON bar_match (start_time);

                CREATE TABLE IF NOT EXISTS bar_match_ally_team (
                    id bigint NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
                    game_id varchar NOT NULL,
                    ally_team_id int NOT NULL,
                    player_count int NOT NULL,
                    start_box_top numeric NOT NULL,
                    start_box_bottom numeric NOT NULL,
                    start_box_left numeric NOT NULL,
                    start_box_right numeric NOT NULL,

                    CONSTRAINT unq_ally_team_game_id_and_ally_team_id UNIQUE (game_id, ally_team_id)
                );

                CREATE INDEX IF NOT EXISTS idx_ally_team_game_id ON bar_match_ally_team (game_id);
            
                CREATE TABLE IF NOT EXISTS bar_match_player (
                    id bigint NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
                    game_id varchar NOT NULL,
                    player_id bigint NOT NULL,
                    user_id bigint NOT NULL,
                    user_name varchar NOT NULL,
                    team_id int NOT NULL,
                    ally_team_id int NOT NULL,
                    faction varchar NOT NULL,
                    starting_position_x numeric NOT NULL,
                    starting_position_y numeric NOT NULL,
                    starting_position_z numeric NOT NULL,
                    skill numeric NOT NULL,
                    skill_uncertainty numeric NOT NULL,
                    color int NOT NULL,
                    handicap numeric NOT NULL,

                    CONSTRAINT unq_player_game_id_player_id UNIQUE (game_id, player_id)
                );

                CREATE INDEX IF NOT EXISTS idx_player_game_id ON bar_match_player (game_id);
                CREATE INDEX IF NOT EXISTS idx_player_user_id ON bar_match_player (user_id);

                CREATE TABLE IF NOT EXISTS bar_match_spectator (
                    id bigint NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
                    game_id varchar NOT NULL,
                    user_id bigint NOT NULL,
                    user_name varchar NOT NULL,
                    
                    CONSTRAINT unq_spectator_game_id_and_user_id UNIQUE (game_id, user_id)
                );

                CREATE INDEX IF NOT EXISTS idx_spectator_game_id ON bar_match_spectator (game_id);

                CREATE TABLE IF NOT EXISTS bar_match_chat_message (
                    id bigint NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
                    game_id varchar NOT NULL,
                    game_timestamp float NOT NULL,
                    to_id smallint NOT NULL,
                    from_id smallint NOT NULL,
                    message varchar NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_chat_message_game_id ON bar_match_chat_message (game_id);
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
