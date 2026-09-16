using gex.Common.Models.Match;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Common.Services.Db.Match {

    public interface IBarMatchAiPlayerDb {
        Task DeleteByGameID(string gameID);
        Task<List<BarMatchAiPlayer>> GetByGameID(string gameID, CancellationToken cancel);
        Task<List<BarMatchAiPlayer>> GetByGameIDs(IEnumerable<string> gameIDs, CancellationToken cancel);
        Task Insert(BarMatchAiPlayer ai, CancellationToken cancel);
    }
}
