using gex.Common.Models.Match;

namespace gex.Common.Services.Db.Match {
    public interface IBarMatchTeamDeathDb {
        Task DeleteByGameID(string gameID);
        Task<List<BarMatchTeamDeath>> GetByGameID(string gameID, CancellationToken cancel);
        Task Insert(BarMatchTeamDeath death, CancellationToken cancel);
    }
}