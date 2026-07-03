using gex.Models.Db;
using gex.Services.Db;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

    public class MatchProcessingWebhookRepository {

        private readonly ILogger<MatchProcessingWebhookRepository> _Logger;
        private readonly MatchProcessingWebhookDb _Db;

        private readonly IMemoryCache _Cache;
        private const string CACHE_KEY = "Gex.MatchProcessingWebhooks.All";

        public MatchProcessingWebhookRepository(ILogger<MatchProcessingWebhookRepository> logger,
            MatchProcessingWebhookDb db, IMemoryCache cache) {

            _Logger = logger;
            _Db = db;
            _Cache = cache;
        }

        public async Task<List<MatchProcessingWebhook>> GetAll(CancellationToken cancel) {
            if (_Cache.TryGetValue(CACHE_KEY, out List<MatchProcessingWebhook>? webhooks) == false || webhooks == null) {
                webhooks = await _Db.GetAll(cancel);

                _Cache.Set(CACHE_KEY, webhooks, new MemoryCacheEntryOptions() {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                });
            }

            return webhooks;
        }

        public async Task<MatchProcessingWebhook?> Get(string url, string type, CancellationToken cancel) {
            return await _Db.Get(url, type, cancel);
        }

        public async Task Upsert(MatchProcessingWebhook webhook, CancellationToken cancel) {
            if (webhook.Type != "parsed" && webhook.Type != "replayed") {
                throw new Exception($"wrong type '{webhook.Type}', expected 'parsed'|'replayed'");
            }

            _Cache.Remove(CACHE_KEY);
            await _Db.Upsert(webhook, cancel);
        }

        public async Task Delete(MatchProcessingWebhook webhook, CancellationToken cancel) {
            _Cache.Remove(CACHE_KEY);
            await _Db.Delete(webhook, cancel);
        }

    }
}
