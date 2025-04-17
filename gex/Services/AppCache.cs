using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using gex.Models.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace gex.Services {

    /// <summary>
    ///     a cache that tracks what keys are cached and contains metadata about them.
    ///     internally uses a <see cref="MemoryCache"/>
    /// </summary>
    public class AppCache : IMemoryCache {

        private readonly MemoryCache _BackingCache;

        private readonly ILogger<AppCache> _Logger;

        private static HashSet<string> TrackedKeys { get; } = new();

        private static readonly IDictionary<string, CacheEntryMetadata> _Metadata
            = new ConcurrentDictionary<string, CacheEntryMetadata>();

        public AppCache(ILogger<AppCache> logger, IOptions<MemoryCacheOptions> options) {
            options.Value.TrackStatistics = true;
            _BackingCache = new MemoryCache(options);
            _Logger = logger;
        }

        private readonly PostEvictionCallbackRegistration _EvictionCallback = new PostEvictionCallbackRegistration() {
            EvictionCallback = (key, value, reason, state) => {
                if (reason == EvictionReason.Expired) {
                    //Console.WriteLine($"{key.ToString()} expired, removing from tracked and meta");
                    // Force is fine, null keys are not added, checked below
                    lock (TrackedKeys) {
                        TrackedKeys.Remove(key.ToString()!);
                    }
                    _Metadata.Remove(key.ToString()!);
                }
            }
        };

        public int Count => _BackingCache.Count;

        public MemoryCacheStatistics? GetCurrentStatistics() => _BackingCache.GetCurrentStatistics();

        public ICacheEntry CreateEntry(object objKey) {
            string? key = objKey.ToString();
            if (key == null) {
                throw new ArgumentNullException($"{objKey} does not turn into a non-null string when calling .ToString()");
            }

            // Was added successfully, false means the element was not added (already in here)
            // It's fine to add here, O(1) operation
            lock (TrackedKeys) {
                if (TrackedKeys.Add(key) == true && _Metadata.ContainsKey(key) == false) {
                    _Metadata.Add(key, new CacheEntryMetadata() {
                        Key = key,
                        Created = DateTime.UtcNow,
                        LastAccessed = DateTime.UtcNow,
                        Uses = 0
                    });
                }
            }

            ICacheEntry entry = _BackingCache.CreateEntry(objKey);

            entry.PostEvictionCallbacks.Add(_EvictionCallback);

            return entry;
        }

        public void Dispose() {
            TrackedKeys.Clear();
            _Metadata.Clear();
            _BackingCache.Dispose();
        }

        public void Remove(object key) {
            // Force is fine, only objects that have a non-null string from .ToString() are stored
            lock (TrackedKeys) {
                TrackedKeys.Remove(key.ToString()!);
            }
            _BackingCache.Remove(key);
        }

        public bool TryGetValue(object objKey, out object? value) {
            string key = objKey.ToString()
                ?? throw new ArgumentNullException($"{objKey} does not turn into a non-null string when calling .ToString()");

			lock (_Metadata) {
				if (_Metadata.ContainsKey(key) == true) {
					++_Metadata[key].Uses;
					_Metadata[key].LastAccessed = DateTime.UtcNow;
				}
			}

            return _BackingCache.TryGetValue(key, out value);
        }

        public CacheEntryMetadata? GetMetadata(string key) { 
            if (_Metadata.ContainsKey(key)) {
                return _Metadata[key];
            }
            return null;
        }

        public HashSet<string> GetTrackedKeys() {
            lock (TrackedKeys) {
                return new HashSet<string>(TrackedKeys);
            }
        }

    }
}
