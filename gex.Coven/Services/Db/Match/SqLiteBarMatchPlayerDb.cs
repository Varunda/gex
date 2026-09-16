using gex.Common.Code.ExtensionMethods;
using gex.Common.Models.Match;
using gex.Common.Services.Db;
using gex.Common.Services.Db.Match;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven.Services.Db.Match;

public class SqLiteBarMatchPlayerDb : IBarMatchPlayerDb {

    private readonly ILogger<SqLiteBarMatchPlayerDb> _Logger;
    private readonly IDbHelper _DbHelper;

    public SqLiteBarMatchPlayerDb(ILogger<SqLiteBarMatchPlayerDb> logger, IDbHelper dbHelper) {
        _Logger = logger;
        _DbHelper = dbHelper;
    }

    public async Task<List<BarMatchPlayer>> GetByGameID(string gameID, CancellationToken cancel) {
        using DbConnection conn = _DbHelper.Connection();
        return await conn.QueryListAsync<BarMatchPlayer>(
            "SELECT * FROM bar_match_player WHERE game_id = @GameID",
            new { GameID = gameID },
            cancel
        );
    }

    public Task<List<BarMatchPlayer>> GetByGameIDs(IEnumerable<string> IDs, CancellationToken cancel) {
        throw new NotImplementedException();
    }

    public Task<List<BarMatchPlayer>> GetByUserID(long userID, CancellationToken cancel) {
        throw new NotImplementedException();
    }

    public async Task Insert(BarMatchPlayer player) {
        using DbConnection conn = _DbHelper.Connection(SqLiteDb.WRITE);
        using DbCommand cmd = await _DbHelper.Command(conn, @"
            INSERT INTO bar_match_player (
                game_id, player_id, team_id, 
                user_id, user_name, ally_team_id, 
                skill, skill_uncertainty
            ) VALUES (
                @GameID, @PlayerID, @TeamID,
                @UserID, @Username, @AllyTeamID,
                @Skill, @SkillUncertainty
            );
        ");

        cmd.AddParameter("GameID", player.GameID);
        cmd.AddParameter("PlayerID", player.PlayerID);
        cmd.AddParameter("TeamID", player.TeamID);
        cmd.AddParameter("UserID", player.UserID);
        cmd.AddParameter("Username", player.Name);
        cmd.AddParameter("AllyTeamID", player.AllyTeamID);
        cmd.AddParameter("Skill", player.Skill);
        cmd.AddParameter("SkillUncertainty", player.SkillUncertainty);
        await cmd.PrepareAsync();

        await cmd.ExecuteNonQueryAsync();
    }

    public Task DeleteByGameID(string gameID) {
        throw new NotImplementedException();
    }
}
