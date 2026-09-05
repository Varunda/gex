using gex.Common.Models.Match;

namespace gex.Common.Services.Db.Match {
    public interface IBarMatchChatMessageDb {
        Task DeleteByGameID(string gameID);
        Task<List<BarMatchChatMessage>> GetByGameID(string gameID, CancellationToken cancel);
        Task Insert(BarMatchChatMessage msg);
    }
}