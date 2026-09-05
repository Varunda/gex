using gex.Common.Models.User;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Common.Services.Db {

    public interface IBarUserDb {

        Task<List<BarUser>> GetAll(CancellationToken cancel);
        Task<BarUser?> GetByID(long userID, CancellationToken cancel);
        Task<List<BarUser>> GetByName(string name, CancellationToken cancel);
        Task<List<UserPreviousName>> GetUserNames(long userID, CancellationToken cancel);
        Task<List<UserSearchResult>> SearchByName(string name, bool includePreviousNames, CancellationToken cancel);
        Task Upsert(long userID, BarUser user, CancellationToken cancel);

    }
}