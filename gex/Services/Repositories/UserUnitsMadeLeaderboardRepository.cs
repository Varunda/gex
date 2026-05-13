using gex.Models.Db;
using gex.Models.UserStats;
using gex.Services.Db;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

    public class UserUnitsMadeLeaderboardRepository {

        private readonly ILogger<UserUnitsMadeLeaderboardRepository> _Logger;
        private readonly UserUnitsMadeLeaderboardDb _Db;
        private readonly IMemoryCache _Cache;

        public UserUnitsMadeLeaderboardRepository(ILogger<UserUnitsMadeLeaderboardRepository> logger,
            UserUnitsMadeLeaderboardDb db, IMemoryCache cache) {

            _Logger = logger;
            _Db = db;
            _Cache = cache;
        }

        public Task<List<UserUnitsMadeLeaderboardEntry>> Get(UserUnitsMadeLeaderboardOptions options, CancellationToken cancel) {
            return _Db.Get(options, cancel);
        }

    }
}
