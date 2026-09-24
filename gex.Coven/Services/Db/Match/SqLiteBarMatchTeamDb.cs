using gex.Common.Code.ExtensionMethods;
using gex.Common.Models.Map;
using gex.Common.Models.Match;
using gex.Common.Services.Db;
using gex.Common.Services.Db.Match;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven.Services.Db.Match {

    public class SqLiteBarMatchTeamDb : IBarMatchTeamDb {

        private readonly ILogger<SqLiteBarMatchTeamDb> _Logger;
        private readonly IDbHelper _DbHelper;
        private readonly IDataReader<BarMatchTeam> _Reader;

        public SqLiteBarMatchTeamDb(ILogger<SqLiteBarMatchTeamDb> logger,
            IDbHelper dbHelper, IDataReader<BarMatchTeam> reader) {

            _Logger = logger;
            _DbHelper = dbHelper;
            _Reader = reader;
        }

        public async Task<List<BarMatchTeam>> GetByGameID(string gameID, CancellationToken cancel) {
            using DbConnection conn = _DbHelper.Connection();
            using DbCommand cmd = await _DbHelper.Command(conn, @"
                SELECT * FROM bar_match_team WHERE game_id = @GameID
            ", cancel);

            cmd.AddParameter("GameID", gameID);
            await cmd.PrepareAsync(cancel);

            return await _Reader.ReadList(cmd, cancel);
        }

        public Task<List<BarMatchTeam>> GetByGameIDs(IEnumerable<string> IDs, CancellationToken cancel) {
            throw new NotImplementedException();
        }

        public Task<List<int>> GetUniqueColors(CancellationToken cancel) {
            throw new NotImplementedException();
        }

        public async Task Insert(BarMatchTeam team, CancellationToken cancel) {
            using DbConnection conn = _DbHelper.Connection(SqLiteDb.WRITE);
            using DbCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO bar_match_team (
                    game_id, team_id, ally_team_id, team_leader_id, faction,
                    starting_position_x, starting_position_y, starting_position_z,
                    color, handicap, start_spot, start_spot_label,
                    opening_lab_unit_definition_name
                ) VALUES (
                    @GameID, @TeamID, @AllyTeamID, @TeamLeaderID, @Faction,
                    @StartPosX, @StartPosY, @StartPosZ,
                    @Color, @Handicap, @StartSpot, @StartSpotLabel,
                    @OpeningLabUnitDef
                );
            ", cancel);

            cmd.AddParameter("GameID", team.GameID);
            cmd.AddParameter("TeamID", team.TeamID);
            cmd.AddParameter("AllyTeamID", team.AllyTeamID);
            cmd.AddParameter("TeamLeaderID", team.TeamLeaderID);
            cmd.AddParameter("Faction", team.Faction);
            cmd.AddParameter("StartPosX", team.StartingPosition.X);
            cmd.AddParameter("StartPosY", team.StartingPosition.Y);
            cmd.AddParameter("StartPosZ", team.StartingPosition.Z);
            cmd.AddParameter("Color", team.Color);
            cmd.AddParameter("Handicap", team.Handicap);
            cmd.AddParameter("StartSpot", team.StartSpot);
            cmd.AddParameter("StartSpotLabel", team.StartSpotLabel);
            cmd.AddParameter("OpeningLabUnitDef", team.OpeningLabUnitDefinitionName);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
        }

        public Task UpdateStartSpot(BarMatchTeam team, CancellationToken cancel) {
            throw new NotImplementedException();
        }

        public Task UpdateStartSpotRole(StartSpotSideStartRoleOverride @override, CancellationToken cancel) {
            throw new NotImplementedException();
        }

        public Task DeleteByGameID(string gameID) {
            throw new NotImplementedException();
        }
    }
}
