using gex.Code.ExtensionMethods;
using gex.Models.Db;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.Match {

    public class BarMatchPlayerDb {

        private readonly ILogger<BarMatchPlayerDb> _Logger;
        private readonly IDbHelper _DbHelper;
        private readonly IDataReader<BarMatchPlayer> _Reader;

        public BarMatchPlayerDb(ILogger<BarMatchPlayerDb> logger,
            IDbHelper dbHelper, IDataReader<BarMatchPlayer> reader) {

            _Logger = logger;
            _DbHelper = dbHelper;
            _Reader = reader;
        }

        public async Task Insert(BarMatchPlayer player) {
            if (string.IsNullOrEmpty(player.GameID)) {
                throw new ArgumentException($"GameID of player is missing!");
            }

            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO bar_match_player (
                    game_id, player_id, user_id, user_name, team_id, ally_team_id, faction,
                    starting_position_x, starting_position_y, starting_position_z,
                    skill, skill_uncertainty,
                    color, handicap
                ) VALUES (
                    @GameID, @PlayerID, @UserID, @Username, @TeamID, @AllyTeamID, @Faction,
                    @StartingPositionX, @StartingPositionY, @StartingPositionZ, 
                    @Skill, @SkillUncertainty,
                    @Color, @Handicap
                );
            ");

            cmd.AddParameter("GameID", player.GameID);
            cmd.AddParameter("PlayerID", player.PlayerID);
            cmd.AddParameter("UserID", player.UserID);
            cmd.AddParameter("Username", player.Name);
            cmd.AddParameter("TeamID", player.TeamID);
            cmd.AddParameter("AllyTeamID", player.AllyTeamID);
            cmd.AddParameter("Faction", player.Faction);
            cmd.AddParameter("StartingPositionX", player.StartingPosition.X);
            cmd.AddParameter("StartingPositionY", player.StartingPosition.Y);
            cmd.AddParameter("StartingPositionZ", player.StartingPosition.Z);
            cmd.AddParameter("Skill", player.Skill);
            cmd.AddParameter("SkillUncertainty", player.SkillUncertainty);
            cmd.AddParameter("Color", player.Color);
            cmd.AddParameter("Handicap", player.Handicap);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

        public async Task<List<BarMatchPlayer>> GetByGameID(string gameID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM bar_match_player
                    WHERE game_id = @GameID
            ");

            cmd.AddParameter("GameID", gameID);
            await cmd.PrepareAsync(cancel);

            List<BarMatchPlayer> players = await _Reader.ReadList(cmd, cancel);
            await conn.CloseAsync();

            return players;
        }

        public async Task<List<BarMatchPlayer>> GetByUserID(long userID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM bar_match_player
                    WHERE user_id = @UserID;
            ");

            cmd.AddParameter("UserID", userID);
            await cmd.PrepareAsync(cancel);

            List<BarMatchPlayer> players = await _Reader.ReadList(cmd, cancel);
            await conn.CloseAsync();

            return players;
        }

        public async Task DeleteByGameID(string gameID) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                DELETE FROM bar_match_player
                    WHERE game_id = @GameID;
            ");

            cmd.AddParameter("GameID", gameID);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }


    }
}
