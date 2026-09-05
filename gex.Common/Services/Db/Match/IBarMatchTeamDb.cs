using gex.Common.Models.Map;
using gex.Common.Models.Match;

namespace gex.Common.Services.Db.Match {
    public interface IBarMatchTeamDb {
        Task DeleteByGameID(string gameID);
        Task<List<BarMatchTeam>> GetByGameID(string gameID, CancellationToken cancel);
        Task<List<BarMatchTeam>> GetByGameIDs(IEnumerable<string> IDs, CancellationToken cancel);
        Task<List<int>> GetUniqueColors(CancellationToken cancel);
        Task Insert(BarMatchTeam team, CancellationToken cancel);
        Task UpdateStartSpot(BarMatchTeam team, CancellationToken cancel);
        Task UpdateStartSpotRole(StartSpotSideStartRoleOverride @override, CancellationToken cancel);
    }
}