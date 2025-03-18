using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch02AddGameEventTables : IDbPatch {
        public int MinVersion => 2;
        public string Name => "add game events tables";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await helper.Command(conn, @"

                CREATE TABLE IF NOT EXISTS bar_match_processing (
                    game_id varchar NOT NULL PRIMARY KEY,
                    demofile_fetched timestamptz NULL,
                    demofile_parsed timestamptz NULL,
                    headless_ran timestamptz NULL,
                    actions_parsed timestamptz NULL
                );

                CREATE TABLE IF NOT EXISTS unit_def_set_entry (
                    hash varchar NOT NULL,
                    definition_id int NOT NULL,
                    definition_name varchar NOT NULL,
                    name varchar NOT NULL,
                    tooltip varchar NOT NULL,
                    metal_cost double precision NOT NULL,
                    energy_cost double precision NOT NULL,
                    health double precision NOT NULL,
                    speed double precision NOT NULL,
                    build_time double precision NOT NULL,
                    unit_group varchar NOT NULL,
                    build_power double precision NOT NULL,
                    metal_make double precision NOT NULL,
                    is_metal_extractor boolean NOT NULL,
                    extracts_metal double precision NOT NULL,
                    metal_storage double precision NOT NULL,
                    wind_generator double precision NOT NULL,
                    tidal_generator double precision NOT NULL,
                    energy_production double precision NOT NULL,
                    energy_upkeep double precision NOT NULL,
                    energy_storage double precision NOT NULL,
                    energy_conversion_capacity double precision NOT NULL,
                    energy_conversion_efficiency double precision NOT NULL,
                    sight_distance double precision NOT NULL,
                    air_sight_distance double precision NOT NULL,
                    attack_range double precision NOT NULL,
                    is_commander boolean NOT NULL,
                    is_reclaimer boolean NOT NULL,
                    is_factory boolean NOT NULL,
                    weapon_count int NOT NULL,
        
                    PRIMARY KEY (hash, definition_id)
                );

                CREATE TABLE IF NOT EXISTS game_id_to_unit_def_hash (
                    game_id varchar PRIMARY KEY,
                    hash varchar NOT NULL
                );

                CREATE TABLE IF NOT EXISTS game_event_unit_created (
                    id bigint NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
                    game_id varchar NOT NULL,
                    frame bigint NOT NULL,
                    unit_id int NOT NULL,
                    team_id int NOT NULL,
                    definition_id int NOT NULL,
                    unit_x decimal NOT NULL,
                    unit_y decimal NOT NULL,
                    unit_z decimal NOT NULL,

                    CONSTRAINT unq_unit_created_game_id_frame_unit_id UNIQUE (game_id, frame, unit_id)
                );

                CREATE INDEX IF NOT EXISTS idx_event_unit_created ON game_event_unit_created (game_id);

                CREATE TABLE IF NOT EXISTS game_event_unit_killed (
                    id bigint NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
                    game_id varchar NOT NULL,
                    frame bigint NOT NULL,
                    unit_id int NOT NULL,
                    team_id int NOT NULL,
                    definition_id int NOT NULL,
                    killed_x decimal NOT NULL,
                    killed_y decimal NOT NULL,
                    killed_z decimal NOT NULL,
                    attacker_id int NULL,
                    attacker_team int NULL,
                    attacker_definition_id int NULL,
                    weapon_definition_id int NOT NULL,
                    attacker_x decimal NULL,
                    attacker_y decimal NULL,
                    attacker_z decimal NULL,

                    CONSTRAINT unq_unit_killed_game_id_frame_unit_id UNIQUE (game_id, frame, unit_id)
                );

                CREATE INDEX IF NOT EXISTS idx_event_unit_killed ON game_event_unit_killed (game_id);

                CREATE TABLE IF NOT EXISTS game_event_team_stats (
                    id bigint NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
                    game_id varchar NOT NULL,
                    frame bigint NOT NULL,
                    team_id int NOT NULL,

					metal_produced double precision NOT NULL,
                    metal_used double precision NOT NULL,
					metal_excess double precision NOT NULL,
                    metal_sent double precision NOT NULL,
					metal_received double precision NOT NULL,

					energy_produced double precision NOT NULL,
					energy_excess double precision NOT NULL,
					energy_sent double precision NOT NULL,
					energy_used double precision NOT NULL,
					energy_received double precision NOT NULL,

					damage_dealt double precision NOT NULL,
					damage_received double precision NOT NULL,

                    units_received int NOT NULL,
					units_produced int NOT NULL,
					units_killed int NOT NULL,
					units_sent int NOT NULL,
					units_captured int NOT NULL,
					units_out_captured int NOT NULL,

                    CONSTRAINT unq_team_stats_game_id_frame_team_id UNIQUE (game_id, frame, team_id)
                );

                CREATE INDEX IF NOT EXISTS idx_event_team_stats_game_id ON game_event_team_stats (game_id);
            
                CREATE TABLE IF NOT EXISTS game_event_army_value_update (
                    id bigint NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
                    game_id varchar NOT NULL,
                    frame bigint NOT NULL,
                    team_id int NOT NULL,
                    value bigint NOT NULL,

                    CONSTRAINT unq_army_value_update_game_id_frame_team_id UNIQUE (game_id, frame, team_id)
                );

                CREATE INDEX IF NOT EXISTS idx_event_army_value_update_game_id ON game_event_army_value_update (game_id);
    
                CREATE TABLE IF NOT EXISTS game_event_unit_taken (
                    id bigint NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
                    game_id varchar NOT NULL,
                    frame bigint NOT NULL,
                    unit_id int NOT NULL,
                    team_id int NOT NULL,
                    new_team_id int NULL,
                    definition_id int NOT NULL,
                    unit_x double precision NOT NULL,
                    unit_y double precision NOT NULL,
                    unit_z double precision NOT NULL,
                
                    CONSTRAINT unq_unit_taken_game_id_frame_unit_id UNIQUE (game_id, frame, unit_id)
                );

                CREATE INDEX IF NOT EXISTS idx_event_unit_taken ON game_event_unit_taken (game_id);

                CREATE TABLE IF NOT EXISTS game_event_unit_given (
                    id bigint NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
                    game_id varchar NOT NULL,
                    frame bigint NOT NULL,
                    unit_id int NOT NULL,
                    team_id int NOT NULL,
                    new_team_id int NOT NULL,
                    definition_id int NOT NULL,
                    unit_x double precision NOT NULL,
                    unit_y double precision NOT NULL,
                    unit_z double precision NOT NULL,
                
                    CONSTRAINT unq_unit_given_game_id_frame_unit_id UNIQUE (game_id, frame, unit_id)
                );

                CREATE INDEX IF NOT EXISTS idx_event_unit_given ON game_event_unit_given (game_id);

                CREATE TABLE IF NOT EXISTS game_event_wind_update (
                    id bigint NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
                    game_id varchar NOT NULL,
                    frame bigint NOT NULL,
                    value double precision NOT NULL,

                    CONSTRAINT unq_wind_update_game_id_frame UNIQUE (game_id, frame)
                );

                CREATE INDEX IF NOT EXISTS idx_event_wind_update_game_id ON game_event_wind_update (game_id);

                CREATE TABLE IF NOT EXISTS game_event_commander_position_update (
                    id bigint NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
                    game_id varchar NOT NULL,
                    frame bigint NOT NULL,
                    unit_id int NOT NULL,
                    unit_x double precision NOT NULL,
                    unit_y double precision NOT NULL,
                    unit_z double precision NOT NULL,

                    CONSTRAINT unq_commander_position_game_id_frame_unit_id UNIQUE (game_id, frame, unit_id)
                );

                CREATE INDEX IF NOT EXISTS idx_event_commander_position_game_id ON game_event_commander_position_update (game_id);

                CREATE TABLE IF NOT EXISTS game_event_unit_transport_loaded (
                    id bigint NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
                    game_id varchar NOT NULL,
                    frame bigint NOT NULL,
                    unit_id int NOT NULL,
                    team_id int NOT NULL,
                    definition_id int NOT NULL,
                    transport_unit_id int NOT NULL,
                    transport_team_id int NOT NULL,
                    unit_x double precision NOT NULL,
                    unit_y double precision NOT NULL,
                    unit_z double precision NOT NULL,

                    CONSTRAINT unq_transport_loaded_game_id_frame_unit_id UNIQUE (game_id, frame, unit_id)
                );

                CREATE INDEX IF NOT EXISTS idx_event_transport_loaded_game_id ON game_event_unit_transport_loaded (game_id);

                CREATE TABLE IF NOT EXISTS game_event_unit_transport_unloaded (
                    id bigint NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
                    game_id varchar NOT NULL,
                    frame bigint NOT NULL,
                    unit_id int NOT NULL,
                    team_id int NOT NULL,
                    definition_id int NOT NULL,
                    transport_unit_id int NOT NULL,
                    transport_team_id int NOT NULL,
                    unit_x double precision NOT NULL,
                    unit_y double precision NOT NULL,
                    unit_z double precision NOT NULL,

                    CONSTRAINT unq_transport_unloaded_game_id_frame_unit_id UNIQUE (game_id, frame, unit_id)
                );

                CREATE INDEX IF NOT EXISTS idx_event_transport_unloaded_game_id ON game_event_unit_transport_unloaded (game_id);

                CREATE TABLE IF NOT EXISTS game_event_factory_unit_created (
                    id bigint NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
                    game_id varchar NOT NULL,
                    frame bigint NOT NULL,
                    unit_id int NOT NULL,
                    team_id int NOT NULL,
                    definition_id int NOT NULL,
                    factory_unit_id int NOT NULL,
                    factory_definition_id int NOT NULL,
                    
                    CONSTRAINT unq_factory_unit_created_game_id_frame_unit_id UNIQUE (game_id, frame, unit_id)
                );

                CREATE INDEX IF NOT EXISTS idx_factory_unit_created_game_id ON game_event_factory_unit_created (game_id);
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
