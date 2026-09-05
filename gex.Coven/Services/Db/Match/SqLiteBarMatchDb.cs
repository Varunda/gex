using Avalonia.Metadata;
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

public class SqLiteBarMatchDb : IBarMatchDb {

    private readonly ILogger<SqLiteBarMatchDb> _Logger;
    private readonly IDbHelper _DbHelper;
    private readonly IDataReader<BarMatch> _Reader;

    public SqLiteBarMatchDb(ILogger<SqLiteBarMatchDb> logger,
        IDbHelper dbHelper, IDataReader<BarMatch> reader) {

        _Logger = logger;
        _DbHelper = dbHelper;
        _Reader = reader;
    }

    public async Task<List<BarMatch>> GetAll(CancellationToken cancel) {
        using DbConnection conn = _DbHelper.Connection();
        using DbCommand cmd = await _DbHelper.Command(conn, @"SELECT * FROM bar_match;", cancel);

        List<BarMatch> matches = await _Reader.ReadList(cmd, cancel);
        await conn.CloseAsync();

        return matches;
    }

    public Task<List<BarMatch>> GetAllByMap(string mapFilename, CancellationToken cancel) {
        throw new NotImplementedException();
    }

    public async Task<BarMatch?> GetByID(string ID, CancellationToken cancel) {
        using DbConnection conn = _DbHelper.Connection();
        using DbCommand cmd = await _DbHelper.Command(conn, @"
            SELECT * FROM bar_match WHERE id = @ID;
        ", cancel);

        cmd.AddParameter("ID", ID);
        await cmd.PrepareAsync(cancel);

        BarMatch? match = await _Reader.ReadSingle(cmd, cancel);
        await conn.CloseAsync();

        return match;
    }

    public Task<List<BarMatch>> GetByIDs(IEnumerable<string> IDs, CancellationToken cancel) {
        throw new NotImplementedException();
    }

    public Task<List<BarMatch>> GetByTimePeriod(DateTime start, DateTime end, CancellationToken cancel) {
        throw new NotImplementedException();
    }

    public Task<List<BarMatch>> GetByUserID(long userID, CancellationToken cancel) {
        throw new NotImplementedException();
    }

    public Task<BarMatch?> GetOldestMatch(CancellationToken cancel) {
        throw new NotImplementedException();
    }

    public Task<List<string>> GetUniqueEngines(CancellationToken cancel) {
        throw new NotImplementedException();
    }

    public Task<List<string>> GetUniqueGameVersions(CancellationToken cancel) {
        throw new NotImplementedException();
    }

    public async Task Insert(BarMatch match, CancellationToken cancel) {
        if (string.IsNullOrEmpty(match.ID)) {
            throw new ArgumentException($"ID of match is empty!");
        }

        using DbConnection conn = _DbHelper.Connection(SqLiteDb.WRITE);
        using DbCommand cmd =  await _DbHelper.Command(conn, @"
            INSERT INTO bar_match (
                id, start_time, start_offset, map, duration_ms, duration_frame_count,
                engine, game_version, file_name, map_name, gamemode, player_count, wrong_skill_values,
                average_os, min_os, max_os, start_spot_version,
                host_settings, game_settings, map_settings, spads_settings, restrictions
            ) VALUES (
                @ID, @StartTime, @StartOffset, @Map, @DurationMs, @DurationFrameCount,
                @Engine, @GameVersion, @FileName, @MapName, @Gamemode, @PlayerCount, @WrongSkillValues,
                @AverageOS, @MinOS, @MaxOS, @StartSpotVersion,
                @HostSettings, @GameSettings, @MapSettings, @SpadsSettings, @Restrictions
            );
        ", cancel);

        cmd.AddParameter("ID", match.ID);
        cmd.AddParameter("StartTime", match.StartTime);
        cmd.AddParameter("StartOffset", match.StartOffset);
        cmd.AddParameter("Map", match.Map);
        cmd.AddParameter("DurationMs", match.DurationMs);
        cmd.AddParameter("DurationFrameCount", match.DurationFrameCount);
        cmd.AddParameter("Engine", match.Engine);
        cmd.AddParameter("GameVersion", match.GameVersion);
        cmd.AddParameter("FileName", match.FileName);
        cmd.AddParameter("MapName", match.MapName);
        cmd.AddParameter("Gamemode", match.Gamemode);
        cmd.AddParameter("PlayerCount", match.PlayerCount);
        cmd.AddParameter("WrongSkillValues", match.WrongSkillValues);
        cmd.AddParameter("AverageOS", match.AverageOS);
        cmd.AddParameter("MinOS", match.MinOS);
        cmd.AddParameter("MaxOS", match.MaxOS);
        cmd.AddParameter("StartSpotVersion", match.StartSpotVersion);

        cmd.AddParameter("HostSettings", match.HostSettings);
        cmd.AddParameter("GameSettings", match.GameSettings);
        cmd.AddParameter("MapSettings", match.MapSettings);
        cmd.AddParameter("SpadsSettings", match.SpadsSettings);
        cmd.AddParameter("Restrictions", match.Restrictions);
        await cmd.PrepareAsync(cancel);

        await cmd.ExecuteNonQueryAsync(cancel);
    }

    public Task<List<BarMatch>> Search(BarMatchSearchParameters parms, int offset, int limit, long? currentUserID, CancellationToken cancel) {
        throw new NotImplementedException();
    }

    public Task Delete(string gameID) {
        throw new NotImplementedException();
    }

    public Task UpdateStartOffset(BarMatch match, CancellationToken cancel) {
        throw new NotImplementedException();
    }

    public Task UpdateStartSpotDataVersion(BarMatch match, CancellationToken cancel) {
        throw new NotImplementedException();
    }

    public Task UpdateWrongSkillValues(BarMatch match, CancellationToken cancel) {
        throw new NotImplementedException();
    }

}
