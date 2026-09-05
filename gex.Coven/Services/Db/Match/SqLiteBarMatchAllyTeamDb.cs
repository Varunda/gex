using gex.Common.Models.Match;
using gex.Common.Services.Db.Match;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven.Services.Db.Match;

public class SqLiteBarMatchAllyTeamDb : IBarMatchAllyTeamDb {

    public Task<List<BarMatchAllyTeam>> GetByGameID(string gameID, CancellationToken cancel) {
        throw new NotImplementedException();
    }

    public Task<List<BarMatchAllyTeam>> GetByGameIDs(IEnumerable<string> gameIDs, CancellationToken cancel) {
        throw new NotImplementedException();
    }

    public Task Insert(BarMatchAllyTeam allyTeam) {
        throw new NotImplementedException();
    }

    public Task DeleteByGameID(string gameID) {
        throw new NotImplementedException();
    }
}
