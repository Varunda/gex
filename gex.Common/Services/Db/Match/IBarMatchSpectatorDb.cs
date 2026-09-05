using gex.Common.Models.Match;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Common.Services.Db.Match {
    public interface IBarMatchSpectatorDb {
        Task DeleteByGameID(string gameID);
        Task<List<BarMatchSpectator>> GetByGameID(string gameID, CancellationToken cancel);
        Task Insert(BarMatchSpectator spec);
    }
}