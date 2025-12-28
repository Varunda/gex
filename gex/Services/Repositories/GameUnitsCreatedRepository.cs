using gex.Models.Event;
using gex.Services.Db.Event;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

    public class GameUnitsCreatedRepository {

        private readonly ILogger<GameUnitsCreatedRepository> _Logger;
        private readonly GameUnitsCreatedDb _Db;
        private readonly BarI18nRepository _I18nRepository;
        private readonly IMemoryCache _Cache;

        private const string CACHE_KEY_GAME = "Gex.GameUnitsCreated.Match.{0}"; // {0} => game ID
        private const string CACHE_KEY_USER = "Gex.GameUnitsCreated.User.{0}"; // {0} => user ID

        public GameUnitsCreatedRepository(ILogger<GameUnitsCreatedRepository> logger,
            GameUnitsCreatedDb db, BarI18nRepository i18nRepository,
            IMemoryCache cache) {

            _Logger = logger;
            _Db = db;
            _I18nRepository = i18nRepository;
            _Cache = cache;
        }

        public async Task<List<GameUnitsCreated>> GetByGameID(string gameID, CancellationToken cancel) {
            string cacheKey = string.Format(CACHE_KEY_GAME, gameID);

            if (_Cache.TryGetValue(cacheKey, out List<GameUnitsCreated>? created) == false || created == null) {
                created = await _Db.GetByGameID(gameID, cancel);

                _Cache.Set(cacheKey, created, new MemoryCacheEntryOptions() {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
                });
            }

            return await _Db.GetByGameID(gameID, cancel);
        }

        public async Task<List<GameUnitsCreated>> GetByUserID(long userID, CancellationToken cancel) {
            string cacheKey = string.Format(CACHE_KEY_USER, userID);

            if (_Cache.TryGetValue(cacheKey, out List<GameUnitsCreated>? created) == false || created == null) {
                created = await _Db.GetByUserID(userID, cancel);

                foreach (GameUnitsCreated m in created) {
                    m.UnitName = await _I18nRepository.GetUnitName(m.DefinitionName, cancel);
                }

                _Cache.Set(cacheKey, created, new MemoryCacheEntryOptions() {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });
            }

            return created;
        }

    }
}
