
--------------------------------------------------------------------
-- notes on splitting the DB into 2 dbs, the main db and event db --
--------------------------------------------------------------------

-- on MAIN
-- create dump:
--		pg_dump -d gex --table=game_event_* --table=unit_def_set_entry --table=game_id_to_unit_def_hash > game_events.psql

-- on EVENTS
-- load dump:
--		psql -d gex_events < game_events.psql

-- on MAIN
ALTER TABLE game_event_commander_position_update RENAME TO game_event_commander_position_update_old;
ALTER TABLE game_event_extra_stats RENAME TO game_event_extra_stats_old;
ALTER TABLE game_event_factory_unit_created RENAME TO game_event_factory_unit_created_old;
ALTER TABLE game_event_team_died RENAME TO game_event_team_died_old;
ALTER TABLE game_event_team_stats RENAME TO game_event_team_stats_old;
ALTER TABLE game_event_unit_created RENAME TO game_event_unit_created_old;
ALTER TABLE game_event_unit_created_copy RENAME TO game_event_unit_created_copy_old;
ALTER TABLE game_event_unit_damage RENAME TO game_event_unit_damage_old;
ALTER TABLE game_event_unit_given RENAME TO game_event_unit_given_old;
ALTER TABLE game_event_unit_killed RENAME TO game_event_unit_killed_old;
ALTER TABLE game_event_unit_position RENAME TO game_event_unit_position_old;
ALTER TABLE game_event_unit_resources RENAME TO game_event_unit_resources_old;
ALTER TABLE game_event_unit_taken RENAME TO game_event_unit_taken_old;
ALTER TABLE game_event_unit_transport_loaded RENAME TO game_event_unit_transport_loaded_old;
ALTER TABLE game_event_unit_transport_unloaded RENAME TO game_event_unit_transport_unloaded_old;
ALTER TABLE game_event_wind_update RENAME TO game_event_wind_update_old;

-- on EVENTS
-- create a working copy of the game_event_unit_created table
CREATE TABLE game_event_unit_created_copy (LIKE game_event_unit_created INCLUDING all);

-- use the view with the definition_name to insert into the working copy of the unit created table
INSERT INTO game_event_unit_created_copy (
	id, game_id, frame, unit_id, team_id, definition_id, definition_name, unit_x, unit_y, unit_z, rotation
) SELECT id, game_id, frame, unit_id, team_id, definition_id, definition_name, unit_x, unit_y, unit_z, rotation
	FROM game_event_unit_created_def
	-- WHERE definition_name IS NOT NULL; -- used on local where some data is in a _funky_ state
	;

-- on EVENTS
-- create foreign data wrapper to main DB for match data
CREATE EXTENSION IF NOT EXISTS postgres_fdw;

CREATE SERVER gex_main
	FOREIGN DATA WRAPPER postgres_fdw
	OPTIONS (host 'localhost', port '5432', dbname 'user');
	
CREATE USER MAPPING FOR user
	SERVER gex_main
	OPTIONS (user 'user', password 'password');
	
IMPORT FOREIGN SCHEMA public
	LIMIT TO (bar_match, bar_match_player, bar_match_ally_team, bar_match_spectator, bar_match_processing)
	FROM SERVER gex_main
	INTO public;

