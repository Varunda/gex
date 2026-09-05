using gex.Common.Models.Match;
using gex.Common.Services.Db.Match;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven.Services.Db.Match;
public class SqLiteBarMatchProcessingDb : IBarMatchProcessingDb {
    public Task<BarMatchProcessing?> GetByGameID(string gameID, CancellationToken cancel) {
        throw new NotImplementedException();
    }

    public Task<BarMatchProcessing?> GetLowestPriority(CancellationToken cancel) {
        throw new NotImplementedException();
    }

    public Task<List<BarMatchProcessing>> GetPending(CancellationToken cancel) {
        throw new NotImplementedException();
    }

    public Task<List<BarMatchProcessing>> GetPriorityList(int count, CancellationToken cancel) {
        throw new NotImplementedException();
    }

    public Task<List<BarMatchProcessing>> NeedsActionLogCompression(CancellationToken cancel) {
        throw new NotImplementedException();
    }

    public Task<List<BarMatchProcessing>> NeedsActionLogDeleted(TimeSpan range, CancellationToken cancel) {
        throw new NotImplementedException();
    }

    public Task<List<BarMatchProcessing>> NeedsTeamsReparse(CancellationToken cancel) {
        throw new NotImplementedException();
    }

    public Task<List<BarMatchProcessing>> NeedsUnitPositionCompression(CancellationToken cancel) {
        throw new NotImplementedException();
    }

    public Task Upsert(BarMatchProcessing proc) {
        throw new NotImplementedException();
    }
}
