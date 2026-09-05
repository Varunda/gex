using gex.Common.Models.Match;

namespace gex.Common.Services.Db.Match {
    public interface IBarMatchPlayerLeftDb {
        Task DeleteByGameID(string gameID);
        Task<List<BarMatchPlayerLeft>> GetByGameID(string gameID, CancellationToken cancel);
        Task Insert(BarMatchPlayerLeft left, CancellationToken cancel);
    }
}