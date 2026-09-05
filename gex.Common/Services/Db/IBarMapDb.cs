using gex.Common.Models.Map;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Common.Services.Db {
    public interface IBarMapDb {
        Task<List<BarMap>> GetAll(CancellationToken cancel);
        Task<BarMap?> GetByFileName(string mapName, CancellationToken cancel);
        Task<BarMap?> GetByID(int mapID, CancellationToken cancel);
        Task<BarMap?> GetByName(string name, CancellationToken cancel);
        Task Upsert(BarMap map, CancellationToken cancel);
    }
}