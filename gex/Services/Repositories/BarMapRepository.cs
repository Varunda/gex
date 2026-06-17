using gex.Common.Code.Constants;
using gex.Common.Models;
using gex.Common.Services.Bar;
using gex.Models.Bar;
using gex.Services.BarApi;
using gex.Services.Db;
using gex.Services.Util;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

    public class BarMapRepository {

        private readonly ILogger<BarMapRepository> _Logger;
        private readonly BarMapApi _Api;
        private readonly BarMapDb _Db;
        private readonly StartSpotDataRepository _StartSpotDataRepository;
        private readonly PrDownloaderService _PrDownloader;
        private readonly MapSymmetryUtil _MapSymmetryUtil;

        private readonly IMemoryCache _Cache;
        private const string CACHE_KEY_NAME = "Gex.BarMap.{0}"; // {0} => map name

        public BarMapRepository(ILogger<BarMapRepository> logger,
            BarMapApi api, BarMapDb db,
            IMemoryCache cache, PrDownloaderService prDownloader,
            StartSpotDataRepository startSpotDataRepository, MapSymmetryUtil mapSymmetryUtil) {

            _Logger = logger;
            _Api = api;
            _Db = db;
            _Cache = cache;
            _PrDownloader = prDownloader;
            _StartSpotDataRepository = startSpotDataRepository;
            _MapSymmetryUtil = mapSymmetryUtil;
        }

        public Task<List<BarMap>> GetAll(CancellationToken cancel) {
            return _Db.GetAll(cancel);
        }

        public Task<BarMap?> GetByName(string name, CancellationToken cancel) {
            return _Db.GetByName(name, cancel);
        }

        public async Task<BarMap?> GetByFileName(string filename, CancellationToken cancel) {
            if (string.IsNullOrEmpty(filename)) {
                return null;
            }

            string cacheKey = string.Format(CACHE_KEY_NAME, filename);
            if (_Cache.TryGetValue(cacheKey, out BarMap? map) == false) {
                _Logger.LogTrace($"loading bar map from DB [filename={filename}]");
                map = await _Db.GetByFileName(filename, cancel);

                if (map == null) {
                    _Logger.LogDebug($"loading map info from BAR api [filename={filename}]");
                    Result<BarMap, string> api = await _Api.GetByName(filename, cancel);

                    if (api.IsOk == true) {
                        map = api.Value;

                        Result<MapSymmetryAxis, string> symAxis = await _MapSymmetryUtil.Find(filename);
                        if (symAxis.IsOk == true) {
                            map.SymmetryAxis = symAxis.Value;
                        } else {
                            _Logger.LogWarning($"error when getting symmetry axis of map [map={filename}] [error={symAxis.Error}]");
                        }

                        await _Db.Upsert(api.Value, cancel);
                    } else {
                        _Logger.LogWarning($"failed to load map from API [filename={filename}] [error={api.Error}]");
                    }
                }

                _Logger.LogTrace($"put bar map into cache [filename={filename}] [map.name={map?.Name}]");
                _Cache.Set(cacheKey, map, new MemoryCacheEntryOptions() {
                    SlidingExpiration = TimeSpan.FromMinutes(6)
                });
            }

            return map;
        }

        public async Task Upsert(BarMap map, CancellationToken cancel) {
            await _Db.Upsert(map, cancel);
        }

    }
}
