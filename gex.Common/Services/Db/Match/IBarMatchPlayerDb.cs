using gex.Common.Models.Match;

namespace gex.Common.Services.Db.Match {
    public interface IBarMatchPlayerDb {
        Task DeleteByGameID(string gameID);
        Task<List<BarMatchPlayer>> GetByGameID(string gameID, CancellationToken cancel);
        Task<List<BarMatchPlayer>> GetByGameIDs(IEnumerable<string> IDs, CancellationToken cancel);
        Task<List<BarMatchPlayer>> GetByUserID(long userID, CancellationToken cancel);
        Task Insert(BarMatchPlayer player);
    }
}