using gex.Common.Models;
using gex.Models.Bar;
using gex.Models.Db;
using gex.Models.Map;
using gex.Services.Db.Match;
using gex.Services.Parser;
using gex.Services.Repositories;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Migrations {

    public class BarMatchPlayerStartSpotMigration {

        private readonly ILogger<BarMatchPlayerStartSpotMigration> _Logger;
        private readonly BarMapRepository _MapRepository;
        private readonly BarMatchDb _MatchDb;
        private readonly BarMatchPlayerDb _MatchPlayerDb;
        private readonly BarMatchAllyTeamDb _MatchAllyTeamDb;
        private readonly StartSpotDataRepository _StartSpotDataRepository;

        public BarMatchPlayerStartSpotMigration(ILogger<BarMatchPlayerStartSpotMigration> logger,
            BarMapRepository mapRepository, BarMatchDb matchDb,
            BarMatchPlayerDb matchPlayerDb, StartSpotDataRepository startSpotDataRepository,
            BarMatchAllyTeamDb matchAllyTeamDb) {

            _Logger = logger;
            _MapRepository = mapRepository;
            _MatchDb = matchDb;
            _MatchPlayerDb = matchPlayerDb;
            _StartSpotDataRepository = startSpotDataRepository;
            _MatchAllyTeamDb = matchAllyTeamDb;
        }

        /// <summary>
        ///     fix the player start spot data for all matches 
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task FixAll(CancellationToken cancel) {
            List<BarMap> maps = await _MapRepository.GetAll(cancel);

            foreach (BarMap map in maps) {
                _Logger.LogDebug($"fixing player start for map [map={map.FileName}]");
                await FixMap(map, cancel);
            }
        }

        /// <summary>
        ///     fix the player start spot data for all matches on a map
        /// </summary>
        /// <param name="map"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task FixMap(BarMap map, CancellationToken cancel) {
            StartSpotData? data = await _StartSpotDataRepository.GetLatestByMapFilename(map.FileName, cancel);
            if (data == null) {
                _Logger.LogWarning($"cannot fix match players for map, start position data is null [map={map.FileName}]");
                return;
            }

            Stopwatch timer = Stopwatch.StartNew();
            Stopwatch stepTimer = Stopwatch.StartNew();

            long parseStartSpotsMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

            List<BarMatch> matches = await _MatchDb.GetAllByMap(map.FileName, cancel);
            _Logger.LogDebug($"loaded matches to fix player start positions for [mapName={map.FileName}] [count={matches.Count}]");
            long matchDbMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

            List<BarMatchPlayer> players = await _MatchPlayerDb.GetByGameIDs(matches.Select(iter => iter.ID), cancel);
            Dictionary<string, List<BarMatchPlayer>> playerDict = [];
            foreach (BarMatchPlayer p in players) {
                if (playerDict.ContainsKey(p.GameID) == false) {
                    playerDict.Add(p.GameID, new List<BarMatchPlayer>());
                }

                playerDict.GetValueOrDefault(p.GameID)!.Add(p);
            }
            long playerDbMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

            List<BarMatchAllyTeam> allyTeams = await _MatchAllyTeamDb.GetByGameIDs(matches.Select(iter => iter.ID), cancel);
            Dictionary<string, List<BarMatchAllyTeam>> allyTeamDict = [];
            foreach (BarMatchAllyTeam at in allyTeams) {
                if (allyTeamDict.ContainsKey(at.GameID) == false) {
                    allyTeamDict.Add(at.GameID, new List<BarMatchAllyTeam>());
                }

                allyTeamDict.GetValueOrDefault(at.GameID)!.Add(at);
            }

            foreach (BarMatch match in matches) {
                List<BarMatchPlayer> matchPlayers = playerDict.GetValueOrDefault(match.ID) ?? [];
                match.AllyTeams = allyTeamDict.GetValueOrDefault(match.ID) ?? [];
                await FixMatch(match, matchPlayers, data, cancel);
            }

            long updateMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

            _Logger.LogInformation($"fixed player start spot data [map={map.FileName}] [count={matches.Count}] [timer={timer.ElapsedMilliseconds}ms] "
                + $"[start spot parse={parseStartSpotsMs}ms] [match db load={matchDbMs}ms] [player db load={playerDbMs}ms] [update={updateMs}ms]");
        }

        /// <summary>
        ///     fix the player start spot data of a single match
        /// </summary>
        /// <param name="match"></param>
        /// <param name="players"></param>
        /// <param name="data"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task FixMatch(BarMatch match, List<BarMatchPlayer> players, StartSpotData data, CancellationToken cancel) {
            if (players.Count == 0) {
                _Logger.LogWarning($"missing players for match [gameID={match.ID}]");
                return;
            }

            int missing = 0;
            int found = 0;

            foreach (BarMatchPlayer player in players) {
                StartSpotSideStart? nearestSpot = data.GetNearestStartSpot(match.AllyTeams.Count, player.StartingPosition.X, player.StartingPosition.Z);

                if (nearestSpot == null) {
                    ++missing;
                    continue;
                }

                ++found;

                player.StartSpot = nearestSpot.SpawnPoint;
                player.StartSpotLabel = nearestSpot.Role;

                await _MatchPlayerDb.UpdateStartSpot(player, cancel);
            }
        }

    }
}
