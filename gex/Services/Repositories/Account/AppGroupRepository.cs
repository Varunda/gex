using gex.Models.Internal;
using gex.Services.Db.Account;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories.Account {

    public class AppGroupRepository {

        private readonly ILogger<AppGroupRepository> _Logger;
        private readonly AppGroupDb _AppGroupDb;

        public AppGroupRepository(ILogger<AppGroupRepository> logger,
            AppGroupDb appGroupDb) {

            _Logger = logger;
            _AppGroupDb = appGroupDb;
        }

        public Task<List<AppGroup>> GetAll(CancellationToken cancel) {
            return _AppGroupDb.GetAll(cancel);
        }

        public Task<AppGroup?> GetByID(long groupID, CancellationToken cancel) {
            return _AppGroupDb.GetByID(groupID, cancel);
        }

        public Task<long> Insert(AppGroup group, CancellationToken cancel) {
            return _AppGroupDb.Insert(group, cancel);
        }

        public Task Upsert(AppGroup group, CancellationToken cancel) {
            return _AppGroupDb.Upsert(group, cancel);
        }

    }
}
