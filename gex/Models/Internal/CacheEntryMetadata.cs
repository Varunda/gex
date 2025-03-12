using Microsoft.Extensions.Caching.Memory;
using System;

namespace gex.Models.Internal {

    public class CacheEntryMetadata {

        /// <summary>
        ///     key of the item cached
        /// </summary>
        public string Key { get; set; } = "";

        /// <summary>
        ///     when the item represented by this metadata entered the cache
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        ///     when the item represented by this metadata was last accessed
        /// </summary>
        public DateTime LastAccessed { get; set; }

        /// <summary>
        ///     how many times the item represented by this metadata has been accessed
        /// </summary>
        public int Uses { get; set; }

        /// <summary>
        ///     options the item represented by this metadata was created with
        /// </summary>
        public MemoryCacheEntryOptions Options { get; set; } = new();

        public override string ToString() {
            return $"[{nameof(CacheEntryMetadata)}: {nameof(Key)}={Key}, {nameof(Created)}={Created:u}]";
        }

    }
}
