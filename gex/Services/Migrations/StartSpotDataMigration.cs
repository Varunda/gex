using gex.Code.ExtensionMethods;
using gex.Common.Models;
using gex.Models.Bar;
using gex.Models.Db;
using gex.Models.Map;
using gex.Services.Parser;
using gex.Services.Repositories;
using gex.Services.Util;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Migrations {

    public class StartSpotDataMigration {

        private readonly ILogger<StartSpotDataMigration> _Logger;
        private readonly BarMatchRepository _MatchRepository;
        private readonly BarMapRepository _MapRepository;
        private readonly StartSpotDataRepository _StartSpotDataRepository;
        private readonly StartSpotDataParser _PositionsParser;

        public StartSpotDataMigration(ILogger<StartSpotDataMigration> logger,
            BarMatchRepository matchRepository, BarMapRepository mapRepository,
            StartSpotDataParser positionsParser, StartSpotDataRepository startSpotDataRepository) {

            _Logger = logger;
            _MatchRepository = matchRepository;
            _MapRepository = mapRepository;
            _PositionsParser = positionsParser;
            _StartSpotDataRepository = startSpotDataRepository;
        }

        public async Task FixAll(CancellationToken cancel) {
            List<BarMap> maps = await _MapRepository.GetAll(cancel);

            _Logger.LogDebug($"updating the map start position data for maps [all count={maps.Count}]");

            int updatedCount = 0;
            int missingCount = 0;
            int errorCount = 0;

            foreach (BarMap map in maps) {
                _Logger.LogTrace($"updating map start position data [map={map.FileName}]");

                StartSpotData? existing = await _StartSpotDataRepository.GetLatestByMapFilename(map.FileName, cancel);
                if (existing != null) {
                    _Logger.LogInformation($"map start spot data already exists, skipping [map={map.FileName}]");
                    continue;
                }

                Stopwatch timer = Stopwatch.StartNew();

                // get the latest match that has a start pos data set in the game settings
                List<BarMatch> matches = await _MatchRepository.Search(new BarMatchSearchParameters() {
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

                    Result<StartSpotData, string> res = _PositionsParser.Parse(map.FileName, json);
                    if (res.IsOk == false) {
                        _Logger.LogError($"failed to parse json [map={map.FileName}] [match={match.ID}] [error={res.Error}]");
                        ++errorCount;
                        continue;
                    }

                    await _StartSpotDataRepository.Insert(res.Value, cancel);
                    ++updatedCount;
                }

                long parseMs = timer.ElapsedMilliseconds; timer.Restart();

                await _MapRepository.Upsert(map, cancel);
                _Logger.LogTrace($"updated map start position data [db={dbMs}ms] [parse={parseMs}ms]");
            }

            string msg = $"updated map start pos data [updated={updatedCount}] [missing={missingCount}] [error={errorCount}]";
            _Logger.LogInformation(msg);
        }

    }
}
