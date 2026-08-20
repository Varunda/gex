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
                    game_id, player_id, user_id, user_name, team_id, ally_team_id,
                    skill, skill_uncertainty
                ) VALUES (
                    @GameID, @PlayerID, @UserID, @Username, @TeamID, @AllyTeamID,
                    @Skill, @SkillUncertainty
                );
            ");

            cmd.AddParameter("GameID", player.GameID);
            cmd.AddParameter("PlayerID", player.PlayerID);
            cmd.AddParameter("UserID", player.UserID);
            cmd.AddParameter("Username", player.Name);
            cmd.AddParameter("TeamID", player.TeamID);
            cmd.AddParameter("AllyTeamID", player.AllyTeamID);
            cmd.AddParameter("Skill", player.Skill);
            cmd.AddParameter("SkillUncertainty", player.SkillUncertainty);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

        public async Task<List<BarMatchPlayer>> GetByGameID(string gameID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<BarMatchPlayer>(
                @"SELECT * FROM bar_match_player WHERE game_id = @GameID",
                new {
                    GameID = gameID 
                },
                cancel
            );
            /*
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
            */
        }

        /// <summary>
        ///     get a list of <see cref="BarMatchPlayer"/>s based on the <see cref="BarMatchPlayer.GameID"/>
        /// </summary>
        /// <param name="IDs">List of IDs to get from the DB</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        public async Task<List<BarMatchPlayer>> GetByGameIDs(IEnumerable<string> IDs, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM bar_match_player
                    WHERE game_id = ANY(@IDs);
            ", cancel);

            cmd.AddParameter("IDs", IDs.ToList());
            await cmd.PrepareAsync(cancel);

            List<BarMatchPlayer> matches = await _Reader.ReadList(cmd, cancel);
            await conn.CloseAsync();

            return matches;
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
