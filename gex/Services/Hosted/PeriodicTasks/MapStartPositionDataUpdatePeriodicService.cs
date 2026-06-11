using gex.Code.ExtensionMethods;
using gex.Common.Models;
using gex.Models.Bar;
using gex.Models.Db;
using gex.Services.Repositories;
using gex.Services.Util;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.PeriodicTasks {

    public class MapStartPositionDataUpdatePeriodicService : AppBackgroundPeriodicService {

        private readonly BarMatchRepository _MatchRepository;
        private readonly BarMapRepository _MapRepository;

        public MapStartPositionDataUpdatePeriodicService(ILoggerFactory loggerFactory,
            ServiceHealthMonitor healthMon, BarMatchRepository matchRepository,
            BarMapRepository mapRepository)
        : base("map_start_position_data_update_periodic_service", TimeSpan.FromMinutes(15), loggerFactory, healthMon) {

            _MatchRepository = matchRepository;
            _MapRepository = mapRepository;
        }

        protected override async Task<string?> PerformTask(CancellationToken cancel) {

            List<BarMap> maps = await _MapRepository.GetAll(cancel);

            _Logger.LogDebug($"updating the map start position data for maps [all count={maps.Count}]");

            List<BarMap> toUpdate = maps.Where(iter => {
                return DateTime.UtcNow - iter.Timestamp > TimeSpan.FromMinutes(60);
            }).ToList();

            int updatedCount = 0;
            int missingCount = 0;
            int errorCount = 0;

            foreach (BarMap map in toUpdate) {
                _Logger.LogTrace($"updating map start position data [map={map.FileName}]");

                Stopwatch timer = Stopwatch.StartNew();

                // get the latest match that has a start pos data set in the game settings
                List<BarMatch> matches = await _MatchRepository.Search(new Models.Db.BarMatchSearchParameters() {
                    Map = map.Name,
                    GameSettings = [ new SearchKeyValue() {
                        Key = "mapmetadata_startpos",
                        Operation = "ne",
                        Value = ""
                    }],
                    OrderBy = OrderBy.START_TIME,
                    OrderByDirection = OrderByDirection.DESC
                }, offset: 0, limit: 1, currentUser: null, cancel: cancel);

                long dbMs = timer.ElapsedMilliseconds; timer.Restart();

                if (matches.Count <= 0) {
                    ++missingCount;
                    _Logger.LogInformation($"no matches for map have mapmetadata_startpos [map={map.FileName}]");
                } else {
                    BarMatch match = matches[0];
                    JsonElement? startPos = match.GameSettings.GetChild("mapmetadata_startpos");
                    if (startPos == null || startPos.Value.ValueKind == JsonValueKind.Null) {
                        Debug.Fail($"logic failsafe: mapmetadata_startpos is missing for map [map={map.FileName}] [match={match.ID}]");
                        throw new Exception($"logic failsafe: mapmetadata_startpos from match was null but search gave gex results?");
                    }

                    string b64zlib = startPos.Value.GetString() ?? throw new Exception($"mapmetadata_startpos is null");
                    byte[] compressed = Base64Url.DecodeFromChars(b64zlib);

                    Result<byte[], string> decomp = await SafeZLib.Decompress(compressed, 1024 * 1024 * 4, cancel);
                    if (decomp.IsOk == false) {
                        _Logger.LogError($"failed to decompress zlib start position data [map={map.FileName}] [match={match.ID}] [error={decomp.Error}]");
                        ++errorCount;
                        continue;
                    }

                    string jsonStr = Encoding.UTF8.GetString(decomp.Value);
                    JsonElement json = JsonSerializer.Deserialize<JsonElement>(jsonStr);

                    map.StartPositionData = json;
                    ++updatedCount;
                }

                long parseMs = timer.ElapsedMilliseconds; timer.Restart();

                await _MapRepository.Upsert(map, cancel);
                _Logger.LogTrace($"updated map start position data [db={dbMs}ms] [parse={parseMs}ms]");
            }

            string msg = $"updated map start pos data [updated={updatedCount}] [missing={missingCount}] [error={errorCount}]";
            _Logger.LogInformation(msg);
            return msg;
        }

    }
}
