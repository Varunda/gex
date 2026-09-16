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

public class SqLiteBarMatchAllyTeamDb : IBarMatchAllyTeamDb {

    private readonly ILogger<SqLiteBarMatchAllyTeamDb> _Logger;
    private readonly IDbHelper _DbHelper;
    private readonly IDataReader<BarMatchAllyTeam> _Reader;

    public SqLiteBarMatchAllyTeamDb(ILogger<SqLiteBarMatchAllyTeamDb> logger,
        IDbHelper dbHelper, IDataReader<BarMatchAllyTeam> reader) {

        _Logger = logger;
        _DbHelper = dbHelper;
        _Reader = reader;
    }

    public async Task<List<BarMatchAllyTeam>> GetByGameID(string gameID, CancellationToken cancel) {
        using DbConnection conn = _DbHelper.Connection();
        using DbCommand cmd = await _DbHelper.Command(conn,
            @"SELECT * FROM bar_match_ally_team WHERE game_id = @GameID",
            cancel
        );

        cmd.AddParameter("GameID", gameID);
        await cmd.PrepareAsync(cancel);

        return await _Reader.ReadList(cmd, cancel);
    }

    public Task<List<BarMatchAllyTeam>> GetByGameIDs(IEnumerable<string> gameIDs, CancellationToken cancel) {
        throw new NotImplementedException();
    }

    public async Task Insert(BarMatchAllyTeam allyTeam) {
        using DbConnection conn = _DbHelper.Connection(SqLiteDb.WRITE);
        using DbCommand cmd = await _DbHelper.Command(conn, @"
            INSERT INTO bar_match_ally_team (
                game_id, ally_team_id, player_count, won,
                start_box_top, start_box_bottom, start_box_left, start_box_right,
                average_skill
            ) VALUES (
                @GameID, @AllyTeamID, @PlayerCount, @Won,
                @StartBoxTop, @StartBoxBottom, @StartBoxLeft, @StartBoxRight,
                @AverageSkill
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
        cmd.AddParameter("AverageSkill", allyTeam.AverageSkill);
        await cmd.PrepareAsync();

        await cmd.ExecuteNonQueryAsync();
    }

    public Task DeleteByGameID(string gameID) {
        throw new NotImplementedException();
    }
}
