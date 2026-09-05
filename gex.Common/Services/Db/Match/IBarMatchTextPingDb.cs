using gex.Common.Models.Match;

namespace gex.Common.Services.Db.Match {
    public interface IBarMatchTextPingDb {
        Task DeleteByGameID(string gameID);
        Task<List<BarMatchMapDrawPoint>> GetByGameID(string gameID, CancellationToken cancel);
        Task Insert(BarMatchMapDrawPoint point, CancellationToken cancel);
    }
}