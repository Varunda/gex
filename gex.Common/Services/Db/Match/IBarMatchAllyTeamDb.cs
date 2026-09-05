using gex.Common.Models.Match;

namespace gex.Common.Services.Db.Match {
    public interface IBarMatchAllyTeamDb {
        Task DeleteByGameID(string gameID);
        Task<List<BarMatchAllyTeam>> GetByGameID(string gameID, CancellationToken cancel);
        Task<List<BarMatchAllyTeam>> GetByGameIDs(IEnumerable<string> gameIDs, CancellationToken cancel);
        Task Insert(BarMatchAllyTeam allyTeam);
    }
}