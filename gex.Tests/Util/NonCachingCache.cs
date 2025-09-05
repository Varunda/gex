using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Tests.Util {

    public class NonCachingCache : IMemoryCache {

        public ICacheEntry CreateEntry(object key) {
            ICacheEntry entry = new NullCacheEntry();
            return entry;
        }

        public void Dispose() { }

        public void Remove(object key) { }

        public bool TryGetValue(object key, out object? value) {
            value = null;
            return false;
        }

    }

    public class NullCacheEntry : ICacheEntry {

        private object? _Obj;
        public DateTimeOffset? AbsoluteExpiration { get => null; set => _Obj = value; }
        public TimeSpan? AbsoluteExpirationRelativeToNow { get => null; set => _Obj = value; }

        public IList<IChangeToken> ExpirationTokens => [];

        public object Key => "";

        public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks => [];

        public CacheItemPriority Priority { get => CacheItemPriority.Normal; set => _Obj = value; }
        public long? Size { get => null; set => _Obj = value; }
        public TimeSpan? SlidingExpiration { get => null; set => _Obj = value; }
        public object? Value { get => null; set => _Obj = value; }

        public void Dispose() { }

    }

}
