using gex.Common.Models.Match;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Common.Services.Db.Match {
    public interface IBarMatchProcessingDb {
        Task<BarMatchProcessing?> GetByGameID(string gameID, CancellationToken cancel);
        Task<BarMatchProcessing?> GetLowestPriority(CancellationToken cancel);
        Task<List<BarMatchProcessing>> GetPending(CancellationToken cancel);
        Task<List<BarMatchProcessing>> GetPriorityList(int count, CancellationToken cancel);
        Task<List<BarMatchProcessing>> NeedsActionLogCompression(CancellationToken cancel);
        Task<List<BarMatchProcessing>> NeedsActionLogDeleted(TimeSpan range, CancellationToken cancel);
        Task<List<BarMatchProcessing>> NeedsTeamsReparse(CancellationToken cancel);
        Task<List<BarMatchProcessing>> NeedsUnitPositionCompression(CancellationToken cancel);
        Task Upsert(BarMatchProcessing proc);
    }
}