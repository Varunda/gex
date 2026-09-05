using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Common.Services.Db.Match {
    public interface IBarMatchProcessingPriorityDb {
        Task<string?> GetByDiscordID(ulong discordID, CancellationToken cancel);
        Task<List<ulong>> GetByGameID(string gameID, CancellationToken cancel);
        Task Upsert(ulong discordID, string gameID, CancellationToken cancel);
    }
}