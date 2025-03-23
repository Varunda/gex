using gex.Code.ExtensionMethods;
using gex.Models.Db;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.Match {

    public class BarMatchAllyTeamDb {

        private readonly ILogger<BarMatchAllyTeamDb> _Logger;
        private readonly IDbHelper _DbHelper;
        private readonly IDataReader<BarMatchAllyTeam> _Reader;

        public BarMatchAllyTeamDb(ILogger<BarMatchAllyTeamDb> logger,
            IDbHelper dbHelper, IDataReader<BarMatchAllyTeam> reader) {

            _Logger = logger;
            _DbHelper = dbHelper;
            _Reader = reader;
        }

        public async Task Insert(BarMatchAllyTeam allyTeam) {
            if (string.IsNullOrEmpty(allyTeam.GameID)) {
                throw new ArgumentException($"GameID of allyteam is missing!");
            }

            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO bar_match_ally_team (
                    game_id, ally_team_id, player_count, won,
                    start_box_top, start_box_bottom, start_box_left, start_box_right
                ) VALUES (
                    @GameID, @AllyTeamID, @PlayerCount, @Won,
                    @StartBoxTop, @StartBoxBottom, @StartBoxLeft, @StartBoxRight
                );
            ");

            cmd.AddParameter("GameID", allyTeam.GameID);
            cmd.AddParameter("AllyTeamID", allyTeam.AllyTeamID);
            cmd.AddParameter("PlayerCount", allyTeam.PlayerCount);
            cmd.AddParameter("Won", allyTeam.Won);
            cmd.AddParameter("StartBoxTop", allyTeam.StartBox.Top);
            cmd.AddParameter("StartBoxBottom", allyTeam.StartBox.Bottom);
            cmd.AddParameter("StartBoxLeft", allyTeam.StartBox.Left);
            cmd.AddParameter("StartBoxRight", allyTeam.StartBox.Right);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

        public async Task<List<BarMatchAllyTeam>> GetByGameID(string gameID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM bar_match_ally_team
                    WHERE game_id = @GameID
            ");

            cmd.AddParameter("GameID", gameID);
            await cmd.PrepareAsync(cancel);

            List<BarMatchAllyTeam> allyTeams = await _Reader.ReadList(cmd, cancel);
            await conn.CloseAsync();

            return allyTeams;
        }

        public async Task DeleteByGameID(string gameID) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                DELETE FROM bar_match_ally_team
                    WHERE game_id = @GameID;
            ");

            cmd.AddParameter("GameID", gameID);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
