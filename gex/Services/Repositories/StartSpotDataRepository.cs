using gex.Models.Map;
using gex.Services.Db.Map;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

    public class StartSpotDataRepository {

        private readonly ILogger<StartSpotDataRepository> _Logger;

        private readonly StartSpotDataDb _DataDb;
        private readonly StartSpotPositionDb _PositionDb;
        private readonly StartSpotConfigurationDb _ConfigurationDb;
        private readonly StartSpotSideDb _SideDb;
        private readonly StartSpotSideStartDb _SideStartDb;
        private readonly StartSpotSideStartRoleOverrideDb _OverrideDb;

        private readonly IMemoryCache _Cache;
        private const string CACHE_KEY = "StartSpotPositions.Map.{0}.{1}"; // {0} => map filename, {1} => version

        public StartSpotDataRepository(ILogger<StartSpotDataRepository> logger,
            StartSpotPositionDb positionDb, StartSpotConfigurationDb configurationDb,
            StartSpotSideDb sideDb, StartSpotSideStartDb sideStartDb,
            IMemoryCache cache, StartSpotDataDb dataDb,
            StartSpotSideStartRoleOverrideDb overrideDb) {

            _Logger = logger;

            _PositionDb = positionDb;
            _ConfigurationDb = configurationDb;
            _SideDb = sideDb;
            _SideStartDb = sideStartDb;
            _Cache = cache;
            _DataDb = dataDb;
            _OverrideDb = overrideDb;
        }

        public async Task<StartSpotData?> GetLatestByMapFilename(string mapFilename, CancellationToken cancel) {
            string cacheKey = string.Format(CACHE_KEY, mapFilename, "Latest");
            if (_Cache.TryGetValue(cacheKey, out StartSpotData? pos) == false || pos == null) {

                List<StartSpotData> poses = await _DataDb.GetLatestByMapFilename(mapFilename, cancel);
                if (poses.Count == 0) {
                    return null;
                }

                if (poses.Count != 1) {
                    throw new Exception($"found more than 1 start spot data for latest map filename [map={mapFilename}]");
                }

                pos = await _Build(poses[0], cancel);

                _Cache.Set(cacheKey, pos, new MemoryCacheEntryOptions() {
                    SlidingExpiration = TimeSpan.FromMinutes(10)
                });
            }

            return pos;
        }

        public async Task<StartSpotData?> GetByVersionAndMapFilename(string mapFilename, int version, CancellationToken cancel) {
            string cacheKey = string.Format(CACHE_KEY, mapFilename, version);
            if (_Cache.TryGetValue(cacheKey, out StartSpotData? pos) == false || pos == null) {

                List<StartSpotData> poses = await _DataDb.GetByVersionAndMapFilename(mapFilename, version, cancel);
                if (poses.Count == 0) {
                    return null;
                }

                if (poses.Count != 1) {
                    throw new Exception($"found more than 1 start spot data for latest map filename [map={mapFilename}]");
                }

                pos = await _Build(poses[0], cancel);

                _Cache.Set(cacheKey, pos, new MemoryCacheEntryOptions() {
                    SlidingExpiration = TimeSpan.FromMinutes(10)
                });
            }

            return pos;
        }

        private async Task<StartSpotData> _Build(StartSpotData data, CancellationToken cancel) {
            List<StartSpotSideStartRoleOverride> overrides = await _OverrideDb.GetByVersionAndMapFilename(data.MapFilename, data.Version, cancel);

            List<StartSpotPosition> positions = await _PositionDb.GetByVersionAndMapFilename(data.MapFilename, data.Version, cancel);
            foreach (StartSpotPosition pos in positions) {
                StartSpotSideStartRoleOverride? @override = overrides.FirstOrDefault(iter => iter.Position == pos.Name);
                if (@override != null && @override.MaxRadius != null) {
                    pos.MaxRadius = @override.MaxRadius;
                }
            }

            data.Positions = positions.OrderBy(iter => iter.Name).ToList();

            List<StartSpotConfiguration> configs = await _ConfigurationDb.GetByVersionAndMapFilename(data.MapFilename, data.Version, cancel);
            List<StartSpotSide> sides = await _SideDb.GetByVersionAndMapFilename(data.MapFilename, data.Version, cancel);
            List<StartSpotSideStart> sideStarts = await _SideStartDb.GetByVersionAndMapFilename(data.MapFilename, data.Version, cancel);

            data.Configurations = configs;
            foreach (StartSpotConfiguration config in data.Configurations) {
                config.Sides = sides.Where(iter => iter.PlayersPerTeam == config.PlayersPerTeam && iter.TeamCount == config.TeamCount)
                    .OrderBy(iter => iter.Index).ToList();

                foreach (StartSpotSide side in config.Sides) {
                    side.Starts = sideStarts.Where(iter => iter.SideIndex == side.Index)
                        .OrderBy(iter => iter.SpawnPoint).ToList();

                    foreach (StartSpotSideStart start in side.Starts) {
                        StartSpotSideStartRoleOverride? @override = overrides.FirstOrDefault(iter => iter.Position == start.SpawnPoint);
                        if (@override != null) {
                            start.Role = @override.Role;
                        }
                    }
                }
            }

            return data;
        }

        public async Task<StartSpotData> Insert(StartSpotData data, CancellationToken cancel) {
            if (string.IsNullOrWhiteSpace(data.MapFilename) == true) {
                throw new ArgumentException($"{nameof(StartSpotData.MapFilename)} cannot be empty");
            }
            if (data.Version != 0) {
                throw new ArgumentException($"{nameof(StartSpotData.Version)} must be 0 to insert");
            }

            List<StartSpotData> poses = await _DataDb.GetLatestByMapFilename(data.MapFilename, cancel);
            if (poses.Count == 0) {
                data.Version = 1;
                _Logger.LogInformation($"new start spot data found, will be version 1 [map={data.MapFilename}]");
            } else if (poses.Count == 1) {
                StartSpotData existing = await GetLatestByMapFilename(data.MapFilename, cancel)
                    ?? throw new Exception($"logic error: have a dataDb entry but not from repo?");

                if (existing == data) {
                    _Logger.LogError($"the data to insert is the same at the latest version [map={data.MapFilename}]");
                    throw new Exception($"the newly inserted data is the same as the latest version [map={data.MapFilename}]");
                }

                data.Version = poses[0].Version + 1;
                _Logger.LogInformation($"new start spot data will use next highest version [map={data.MapFilename}] [version={data.Version}]");

                _Logger.LogInformation($"updating max timestamp of previous version [map={data.MapFilename}] [version={poses[0].Version}]");
                await _DataDb.UpdateMaxTimestamp(poses[0], DateTime.UtcNow, cancel);
            } else if (poses.Count > 1) {
                throw new Exception($"logic error: there is more than one latest version for map [map={data.MapFilename}] [count={poses.Count}]"); 
            }

            // don't respect cancel token at this point, we want all of this done or none of it
            await _DataDb.Insert(data, CancellationToken.None);

            foreach (StartSpotPosition pos in data.Positions) {
                pos.Version = data.Version;
                await _PositionDb.Insert(pos, CancellationToken.None);
            }

            foreach (StartSpotConfiguration config in data.Configurations) {
                config.Version = data.Version;
                await _ConfigurationDb.Insert(config, CancellationToken.None);

                foreach (StartSpotSide side in config.Sides) {
                    side.Version = data.Version;
                    await _SideDb.Insert(side, CancellationToken.None);

                    foreach (StartSpotSideStart start in side.Starts) {
                        start.Version = data.Version;
                        await _SideStartDb.Insert(start, CancellationToken.None);
                    }
                }
            }

            return data;
        }

        public async Task UpsertStartSpotPositionRoleOverride(StartSpotSideStartRoleOverride @override, CancellationToken cancel) {
            string cacheKey = string.Format(CACHE_KEY, @override.MapFilename, @override.Version);
            _Cache.Remove(cacheKey);
            _Cache.Remove(string.Format(CACHE_KEY, @override.MapFilename, "Latest"));

            await _OverrideDb.Upsert(@override, cancel);
        }

    }
}
