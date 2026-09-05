using gex.Common.Models.Match;
using gex.Common.Services.Db.Match;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven.Services.Db.Match;
public class SqLiteBarMatchPlayerDb : IBarMatchPlayerDb {

    public Task<List<BarMatchPlayer>> GetByGameID(string gameID, CancellationToken cancel) {
        throw new NotImplementedException();
    }

    public Task<List<BarMatchPlayer>> GetByGameIDs(IEnumerable<string> IDs, CancellationToken cancel) {
        throw new NotImplementedException();
    }

    public Task<List<BarMatchPlayer>> GetByUserID(long userID, CancellationToken cancel) {
        throw new NotImplementedException();
    }

    public Task Insert(BarMatchPlayer player) {
        throw new NotImplementedException();
    }

    public Task DeleteByGameID(string gameID) {
        throw new NotImplementedException();
    }
}
