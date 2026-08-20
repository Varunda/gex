using gex.Code.ExtensionMethods;
using gex.Models.Db;
using gex.Models.Map;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.Match {

    public class BarMatchTeamDb {

        private readonly ILogger<BarMatchPlayerDb> _Logger;
        private readonly IDbHelper _DbHelper;
        private readonly IDataReader<BarMatchTeam> _Reader;

        public BarMatchTeamDb(ILogger<BarMatchPlayerDb> logger,
            IDbHelper dbHelper, IDataReader<BarMatchTeam> reader) {

            _Logger = logger;
            _DbHelper = dbHelper;
            _Reader = reader;
        }

        public async Task Insert(BarMatchTeam team, CancellationToken cancel) {
            if (string.IsNullOrEmpty(team.GameID)) {
                throw new ArgumentException($"GameID of team is missing!");
            }

            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO bar_match_team (
                    game_id, team_id, ally_team_id, faction, team_leader_id,
                    starting_position_x, starting_position_y, starting_position_z,
                    color, handicap,
                    start_spot, start_spot_label
                ) VALUES (
                    @GameID, @TeamID, @AllyTeamID, @Faction, @TeamLeaderID,
                    @StartingPositionX, @StartingPositionY, @StartingPositionZ, 
                    @Color, @Handicap,
                    @StartSpot, @StartSpotLabel
                );
            ", cancel);

            cmd.AddParameter("GameID", team.GameID);
            cmd.AddParameter("TeamID", team.TeamID);
            cmd.AddParameter("AllyTeamID", team.AllyTeamID);
            cmd.AddParameter("Faction", team.Faction);
            cmd.AddParameter("TeamLeaderID", team.TeamLeaderID);
            cmd.AddParameter("StartingPositionX", team.StartingPosition.X);
            cmd.AddParameter("StartingPositionY", team.StartingPosition.Y);
            cmd.AddParameter("StartingPositionZ", team.StartingPosition.Z);
            cmd.AddParameter("Color", team.Color);
            cmd.AddParameter("Handicap", team.Handicap);
            cmd.AddParameter("StartSpot", team.StartSpot);
            cmd.AddParameter("StartSpotLabel", team.StartSpotLabel);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

        public async Task UpdateStartSpot(BarMatchTeam team, CancellationToken cancel) {
            if (string.IsNullOrWhiteSpace(team.GameID) == true) {
                throw new ArgumentException($"missing {nameof(BarMatchTeam.GameID)}");
            }

            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                UPDATE bar_match_team
                    SET start_spot = @StartSpot,
                        start_spot_label = @StartSpotLabel
                    WHERE
                        game_id = @GameID
                        AND team_id = @TeamID;
            ", cancel);

            cmd.AddParameter("StartSpot", team.StartSpot);
            cmd.AddParameter("StartSpotLabel", team.StartSpotLabel);
            cmd.AddParameter("GameID", team.GameID);
            cmd.AddParameter("TeamID", team.TeamID);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();

        }

        public async Task<List<BarMatchTeam>> GetByGameID(string gameID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM bar_match_team
                    WHERE game_id = @GameID;
            ");

            cmd.AddParameter("GameID", gameID);
            await cmd.PrepareAsync(cancel);

            List<BarMatchTeam> teams = await _Reader.ReadList(cmd, cancel);
            await conn.CloseAsync();

            return teams;
        }

        /// <summary>
        ///     get a list of <see cref="BarMatchPlayer"/>s based on the <see cref="BarMatchPlayer.GameID"/>
        /// </summary>
        /// <param name="IDs">List of IDs to get from the DB</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        public async Task<List<BarMatchTeam>> GetByGameIDs(IEnumerable<string> IDs, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM bar_match_team
                    WHERE game_id = ANY(@IDs);
            ", cancel);

            cmd.AddParameter("IDs", IDs.ToList());
            await cmd.PrepareAsync(cancel);

            List<BarMatchTeam> matches = await _Reader.ReadList(cmd, cancel);
            await conn.CloseAsync();

            return matches;
        }

        public async Task<List<int>> GetUniqueColors(CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<int>($"SELECT DISTINCT(color) FROM bar_match_team", cancel);
        }

        public async Task UpdateStartSpotRole(StartSpotSideStartRoleOverride @override, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                WITH matches AS (
					SELECT id
					FROM bar_match m
					WHERE m.map_name = @MapFilename
						AND m.start_spot_version = @Version
				)
				UPDATE bar_match_team
                    SET start_spot_label = @Role
                WHERE
                    game_id IN (select id from matches)
					AND start_spot = @Position;
            ", cancel);

            cmd.AddParameter("MapFilename", @override.MapFilename);
            cmd.AddParameter("Role", @override.Role);
            cmd.AddParameter("Version", @override.Version);
            cmd.AddParameter("Position", @override.Position);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

        public async Task DeleteByGameID(string gameID) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                DELETE FROM bar_match_team
                    WHERE game_id = @GameID;
            ");

            cmd.AddParameter("GameID", gameID);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
