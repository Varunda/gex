using gex.Coven.Models.Config;
using gex.Coven.Services.Util;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace gex.Coven.Services {

    public class UserOptionsService {

        private readonly ILogger<UserOptionsService> _Logger;
        private readonly IMemoryCache _Cache;
        private const string CACHE_KEY = "Gex.Coven.UserOptions";

        private static readonly string _Path = Path.Join(ShellUtil.GetWorkingDirectory(), "UserOptions.json");

        public UserOptionsService(ILogger<UserOptionsService> logger,
            IMemoryCache cache) {

            _Logger = logger;
            _Cache = cache;
        }

        public delegate void OptionsUpdatedHandler(object sender, UserOptions options);
        public event OptionsUpdatedHandler? OptionsUpdated;

        /// <summary>
        ///     load the <see cref="UserOptions"/> 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public UserOptions Load() {
            if (_Cache.TryGetValue(CACHE_KEY, out UserOptions? userOptions) == true && userOptions != null) {
                return userOptions;
            }

            if (File.Exists(_Path) == false) {
                try {
                    using FileStream _ = File.Create(_Path);
                } catch (Exception ex) {
                    _Logger.LogError(ex, $"failed to create UserOptions.json [path={_Path}]");
                    return new UserOptions();
                }

                if (File.Exists(_Path) == false) {
                    throw new InvalidOperationException($"after creating UserOptions.json, Exists returns null [path={_Path}]");
                }
            }

            string text = "";
            try {
                text = File.ReadAllText(_Path);
            } catch (Exception ex) {
                _Logger.LogError(ex, $"failed to read UserOptions.json, falling back to default [path={_Path}]");
            }

            userOptions = new();
            try {
                userOptions = JsonSerializer.Deserialize<UserOptions>(text);
            } catch (Exception ex) {
                _Logger.LogError(ex, $"failed to parse UserOptions.json, falling back to default [path={_Path}]");
            }

            if (userOptions == null) {
                throw new InvalidOperationException($"how it userOptions null here");
            }

            _Cache.Set(CACHE_KEY, userOptions, new MemoryCacheEntryOptions() {
                Priority = CacheItemPriority.NeverRemove
            });

            return userOptions;
        }

        /// <summary>
        ///     save a <see cref="UserOptions"/> 
        /// </summary>
        /// <param name="options"></param>
        public void Save(UserOptions options) {
            _Cache.Remove(CACHE_KEY);

            OptionsUpdated?.Invoke(this, options);

            string optionsDir = Path.GetDirectoryName(_Path)!;
            Directory.CreateDirectory(optionsDir);

            string json = JsonSerializer.Serialize(options, new JsonSerializerOptions() {
                WriteIndented = true
            });

            try {
                File.WriteAllText(_Path, json);
            } catch (Exception ex) {
                _Logger.LogError(ex, $"failed to write UserOptions to path [path={_Path}]");
            }
        }

    }
}
