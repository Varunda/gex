using gex.Common.Code.ExtensionMethods;
using gex.Common.Models;
using gex.Common.Models.Map;
using gex.Common.Models.Match;
using gex.Common.Services.Db;
using gex.Common.Services.Db.Match;
using gex.Common.Services.Parser;
using gex.Common.Services.Repositories;
using gex.Common.Services.Repository.Match;
using gex.Common.Services.Util;
using gex.Coven.Models.Config;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven.Services.Util {

    public class CovenBarMatchBuilderUtil : IBarMatchBuilderUtil {

        private readonly ILogger<CovenBarMatchBuilderUtil> _Logger;
        private readonly BarMatchRepository _MatchRepository;
        private readonly BarMatchPlayerRepository _PlayerRepository;
        private readonly IBarMatchAllyTeamDb _AllyTeamDb;
        private readonly BarMatchTeamRepository _TeamRepository;
        private readonly IBarMatchAiPlayerDb _AiPlayerDb;
        private readonly IBarMapDb _BarMapDb;
        private readonly UserOptionsService _UserOptions;
        private readonly BarMapParser _MapParser;
        private readonly StorageUtil _StorageUtil;

        public CovenBarMatchBuilderUtil(ILogger<CovenBarMatchBuilderUtil> logger,
            BarMatchRepository matchRepository, BarMatchPlayerRepository playerRepository,
            IBarMatchAllyTeamDb allyTeamDb, BarMatchTeamRepository teamRepository,
            IBarMatchAiPlayerDb aiPlayerDb, IBarMapDb barMapDb,
            UserOptionsService userOptions, BarMapParser mapParser,
            StorageUtil storageUtil) {

            _MatchRepository = matchRepository;
            _PlayerRepository = playerRepository;
            _AllyTeamDb = allyTeamDb;
            _TeamRepository = teamRepository;
            _AiPlayerDb = aiPlayerDb;
            _Logger = logger;
            _BarMapDb = barMapDb;
            _UserOptions = userOptions;
            _MapParser = mapParser;
            _StorageUtil = storageUtil;
        }

        /// <inheritdoc/>
        public async Task<Result<Maybe<BarMatch>, string>> BuildMatch(string gameID, 
            IBarMatchBuilderUtil.BuildOptions options, long? _, CancellationToken cancel) {

            BarMatch? match = await _MatchRepository.GetByID(gameID, cancel);
            if (match == null) {
                return Maybe<BarMatch>.None();
            }

            if (options.IncludeAllyTeams == true) {
                match.AllyTeams = await _AllyTeamDb.GetByGameID(gameID, cancel);
            }

            if (options.IncludePlayers == true) {
                match.Players = await _PlayerRepository.GetByGameID(gameID, cancel);
            }

            if (options.IncludeTeams == true) {
                match.Teams = await _TeamRepository.GetByGameID(gameID, cancel);
            }

            if (options.IncludeAiPlayers == true) {
                match.AiPlayers = await _AiPlayerDb.GetByGameID(gameID, cancel);
            }

            if (options.IncludeMapData == true) {
                BarMap? map = await _BarMapDb.GetByName(match.Map, cancel);
                if (map == null) {
                    UserOptions userOptions = _UserOptions.Load();
                    _Logger.LogDebug($"map is not in DB, attempting to load from files [map={match.Map}]");
                    string mapName = match.Map;
                    string mapPath = Path.Join(userOptions.InstallFolder, "maps", mapName.EscapeRecoilFilesytemCharacters() + ".sd7");
                    if (File.Exists(mapPath) == false) {
                        mapPath = Path.Join(userOptions.InstallFolder, "maps", mapName.ToLower().EscapeRecoilFilesytemCharacters() + ".sd7");
                    }

                    if (File.Exists(mapPath)) {
                        try {
                            Result<BarMapData, string> mapData = await _MapParser.Parse(mapPath, cancel);
                            if (mapData.IsOk == true) {
                                map = new BarMap(mapData.Value);
                                if (map.Name != match.Map) {
                                    throw new InvalidOperationException($"tried to parse map '{match.Map}', but got '{map.Name}' instead");
                                }

                                await _BarMapDb.Upsert(map, cancel);
                                _Logger.LogInformation($"parsed map, saving to DB [map={mapName}]");
                            } else {
                                _Logger.LogError($"failed to parse map [map={map?.Name}] [mapDir={mapPath}] [error={mapData.Error}]");
                            }
                        } catch (Exception ex) {
                            _Logger.LogError(ex, $"failed to parse map [map={map?.Name}] [mapPath={mapPath}]");

                        }
                    } else {
                        _Logger.LogWarning($"missing map directory [map={map}] [mapDir={mapPath}]");
                    }
                }

                match.MapData = map;
            }

            return Maybe<BarMatch>.Some(match);
        }

    }
}
