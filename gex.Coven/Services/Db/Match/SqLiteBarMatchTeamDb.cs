using gex.Common.Models.Map;
using gex.Common.Models.Match;
using gex.Common.Services.Db.Match;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven.Services.Db.Match;

public class SqLiteBarMatchTeamDb : IBarMatchTeamDb {

    public Task<List<BarMatchTeam>> GetByGameID(string gameID, CancellationToken cancel) {
        throw new NotImplementedException();
    }

    public Task<List<BarMatchTeam>> GetByGameIDs(IEnumerable<string> IDs, CancellationToken cancel) {
        throw new NotImplementedException();
    }

    public Task<List<int>> GetUniqueColors(CancellationToken cancel) {
        throw new NotImplementedException();
    }

    public Task Insert(BarMatchTeam team, CancellationToken cancel) {
        throw new NotImplementedException();
    }

    public Task UpdateStartSpot(BarMatchTeam team, CancellationToken cancel) {
        throw new NotImplementedException();
    }

    public Task UpdateStartSpotRole(StartSpotSideStartRoleOverride @override, CancellationToken cancel) {
        throw new NotImplementedException();
    }

    public Task DeleteByGameID(string gameID) {
        throw new NotImplementedException();
    }
}
