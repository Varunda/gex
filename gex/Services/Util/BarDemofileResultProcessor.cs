using gex.Code.ExtensionMethods;
using gex.Common.Code.Constants;
using gex.Common.Models;
using gex.Models.Bar;
using gex.Models.Db;
using gex.Models.Map;
using gex.Models.Queues;
using gex.Models.UserStats;
using gex.Services.Db;
using gex.Services.Db.Match;
using gex.Services.Db.UserStats;
using gex.Services.Parser;
using gex.Services.Queues;
using gex.Services.Repositories;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.JsonDiffPatch;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Util {

    public class BarDemofileResultProcessor {

        private readonly ILogger<BarDemofileResultProcessor> _Logger;
        private readonly BarMatchRepository _MatchRepository;
        private readonly BarReplayDb _ReplayDb;
        private readonly BarMatchAllyTeamDb _MatchAllyTeamDb;
        private readonly BarMatchSpectatorDb _MatchSpectatorDb;
        private readonly BarMatchChatMessageDb _MatchChatMessageDb;
        private readonly BarMatchPlayerRepository _PlayerRepository;
        private readonly BarMapRepository _BarMapRepository;
        private readonly BarUserRepository _UserRepository;
        private readonly BarUserSkillDb _UserSkillDb;
        private readonly GameVersionUsageDb _GameVersionUsageDb;
        private readonly BarMatchTeamDeathDb _TeamDeathDb;
        private readonly BarMatchPlayerLeftDb _PlayerLeftDb;
        private readonly BarMatchTextPingDb _TextPingDb;
        private readonly StartSpotDataRepository _StartSpotDataRepository;
        private readonly StartSpotDataParser _StartSpotDataParser;
        private readonly BarMatchDb _MatchDb;

        private readonly BaseQueue<UserMapStatUpdateQueueEntry> _MapStatUpdateQueue;
        private readonly BaseQueue<UserFactionStatUpdateQueueEntry> _FactionStatUpdateQueue;

        public BarDemofileResultProcessor(ILogger<BarDemofileResultProcessor> logger,
            BarMatchRepository matchRepository, BarReplayDb replayDb,
            BarMatchAllyTeamDb matchAllyTeamDb, BarMatchSpectatorDb matchSpectatorDb,
            BarMatchChatMessageDb matchChatMessageDb, BarMatchPlayerRepository playerRepository,
            BarMapRepository barMapRepository, BarUserRepository userRepository,
            BarUserSkillDb userSkillDb, GameVersionUsageDb gameVersionUsageDb,
            MapPriorityModDb mapPriorityModDb, BarMatchPriorityCalculator priorityCalculator,
            BaseQueue<HeadlessRunQueueEntry> headlessRunQueue, BaseQueue<UserMapStatUpdateQueueEntry> mapStatUpdateQueue,
            BaseQueue<UserFactionStatUpdateQueueEntry> factionStatUpdateQueue, BarMatchTeamDeathDb teamDeathDb,
            StartSpotDataRepository startSpotDataRepository, StartSpotDataParser startSpotDataParser,
            BarMatchDb matchDb, BarMatchPlayerLeftDb playerLeftDb,
            BarMatchTextPingDb textPingDb) {

            _Logger = logger;

            _MatchRepository = matchRepository;
            _ReplayDb = replayDb;
            _MatchAllyTeamDb = matchAllyTeamDb;
            _MatchSpectatorDb = matchSpectatorDb;
            _MatchChatMessageDb = matchChatMessageDb;
            _PlayerRepository = playerRepository;
            _BarMapRepository = barMapRepository;
            _UserRepository = userRepository;
            _UserSkillDb = userSkillDb;
            _GameVersionUsageDb = gameVersionUsageDb;
            _MapStatUpdateQueue = mapStatUpdateQueue;
            _FactionStatUpdateQueue = factionStatUpdateQueue;
            _TeamDeathDb = teamDeathDb;
            _StartSpotDataRepository = startSpotDataRepository;
            _StartSpotDataParser = startSpotDataParser;
            _MatchDb = matchDb;
            _PlayerLeftDb = playerLeftDb;
            _TextPingDb = textPingDb;
        }

        public async Task Process(BarMatch match, CancellationToken cancel) {

            BarMap? map = await _BarMapRepository.GetByFileName(match.MapName, cancel);
            if (map == null) {
                _Logger.LogWarning($"missing bar map! [map={match.MapName}] [gameID={match.ID}]");
            }

            StartSpotData? startSpotData = map == null ? null : await _StartSpotDataRepository.GetLatestByMapFilename(map.FileName, cancel);
            if (startSpotData != null) {
                (bool valid, int found, int missing) = _CheckStartSpotValidity(match, startSpotData);

                if (valid == false) {
                    _Logger.LogWarning($"ignoring existing start spot data as validity check failed "
                        + $"[map={match.MapName}] [gameID={match.ID}] [found={found}] [missing={missing}]");
                    startSpotData = null;
                }
            }

            StartSpotData? newStartSpotData = null;
            JsonElement? matchStartPos = match.GameSettings.GetChild("mapmetadata_startpos");
            if (matchStartPos != null && matchStartPos.Value.ValueKind != JsonValueKind.Undefined && matchStartPos.Value.GetString() != "") {
                Result<StartSpotData, string> result = await _StartSpotDataParser.ParseB64(match.MapName, matchStartPos.Value.GetString()!, cancel);

                do {
                    if (result.IsOk == false) {
                        _Logger.LogError($"failed to parse start spot data [map={match.MapName}] [gameID={match.ID}] [error={result.Error}]");
                        break;
                    }

                    (bool valid, int found, int missing) = _CheckStartSpotValidity(match, result.Value);
                    if (valid == false) {
                        _Logger.LogWarning($"ignoring mapmetadata_startpos from match as validity check failed "
                            + $"[map={match.MapName}] [gameID={match.ID}] [found={found}] [missing={missing}]");
                        break;
                    }

                    newStartSpotData = result.Value;
                } while (false);
            }

            // pick the start spot data that'll be used for this match
            StartSpotData? pickedStartSpotData = null;
            if (startSpotData == null) {
                if (newStartSpotData != null) {
                    newStartSpotData = await _StartSpotDataRepository.Insert(newStartSpotData, cancel);
                    _Logger.LogDebug($"inserting new start spot data due to missing [map={match.MapName}] [gameID={match.ID}] [version={newStartSpotData.Version}]");
                    Debug.Assert(newStartSpotData.Version > 0);
                }

                pickedStartSpotData = newStartSpotData;
            } else if (startSpotData != null && newStartSpotData != null) {
                if (startSpotData != newStartSpotData) {
                    JsonNode parsedNode = JsonNode.Parse(JsonSerializer.Serialize(newStartSpotData))!;
                    JsonNode existingNode = JsonNode.Parse(JsonSerializer.Serialize(startSpotData.Raw))!;

                    JsonNode? diff = existingNode.Diff(parsedNode);
                    _Logger.LogInformation($"inserting new start spot data due to change [map={match.MapName}] [diff={diff}] [gameID={match.ID}]");

                    newStartSpotData = await _StartSpotDataRepository.Insert(newStartSpotData, cancel);
                    pickedStartSpotData = newStartSpotData;
                } else {
                    pickedStartSpotData = startSpotData;
                }
            } else if (startSpotData != null && newStartSpotData == null) {
                pickedStartSpotData = startSpotData;
            } else {
                Debug.Fail($"unchecked logic state of picking which start spot data to use");
                _Logger.LogError($"unchecked logic state of picking what start spot data to use "
                    + $"[existing={startSpotData != null}] [new={newStartSpotData != null}]");
            }

            if (pickedStartSpotData != null) {
                match.StartSpotVersion = pickedStartSpotData.Version;
                _Logger.LogDebug($"selected start spot data to use for match [gameID={match.ID}] [version={match.StartSpotVersion}]");
            }

            // ally teams
            Stopwatch stepTimer = Stopwatch.StartNew();
            foreach (BarMatchAllyTeam allyTeam in match.AllyTeams) {
                await _MatchAllyTeamDb.Insert(allyTeam);
            }
            long insertAllyTeamsMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

            // players
            foreach (BarMatchPlayer player in match.Players) {
                if (pickedStartSpotData != null) {
                    StartSpotSideStart? startSpot = pickedStartSpotData.GetNearestStartSpot(match.AllyTeams.Count, player.StartingPosition.X, player.StartingPosition.Z);

                    player.StartSpot = startSpot?.SpawnPoint;
                    player.StartSpotLabel = startSpot?.Role;
                }

                await _PlayerRepository.Insert(player);
            }
            long insertPlayersMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

            // spectators
            foreach (BarMatchSpectator spec in match.Spectators) {
                await _MatchSpectatorDb.Insert(spec);

                if (spec.UserID != null && spec.UserIDCanBeWrong == false) {
                    await _UserRepository.Upsert(spec.UserID.Value, new BarUser() {
                        UserID = spec.UserID.Value,
                        Username = spec.Name,
                        LastUpdated = match.StartTime,
                        CountryCode = null
                    }, cancel);
                }
            }
            long insertSpecMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

            // chat messages
            foreach (BarMatchChatMessage msg in match.ChatMessages) {
                await _MatchChatMessageDb.Insert(msg);
            }
            long insertChatMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

            // team deaths
            foreach (BarMatchTeamDeath death in match.TeamDeaths) {
                await _TeamDeathDb.Insert(death, cancel);
            }
            long insertDeathMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

            // player left
            foreach (BarMatchPlayerLeft left in match.PlayerLeaves) {
                await _PlayerLeftDb.Insert(left, cancel);
            }
            long insertPlayerLeftMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

            // map draw label
            foreach (BarMatchMapDraw draw in match.MapDraws) {
                if (draw.Action != "point" || draw is not BarMatchMapDrawPoint point || point.Label == "") {
                    continue;
                }

                point.GameID = match.ID;
                await _TextPingDb.Insert(point, cancel);
            }

            await _MatchRepository.Insert(match, cancel);
            long insertMatchMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

            _Logger.LogInformation($"processed match [ID={match.ID}]"
                + $" [ally team db={insertAllyTeamsMs}ms] [player db={insertPlayersMs}ms]"
                + $" [spec ms={insertSpecMs}ms] [chat db={insertChatMs}ms] [death db={match.TeamDeaths.Count}/{insertDeathMs}ms] [match db={insertMatchMs}ms]");

            bool saidWrongSkillValues = false;
            foreach (BarMatchPlayer player in match.Players) {
                try {
                    _MapStatUpdateQueue.Queue(new UserMapStatUpdateQueueEntry() {
                        UserID = player.UserID,
                        Map = match.Map,
                        Gamemode = match.Gamemode
                    });

                    _FactionStatUpdateQueue.Queue(new UserFactionStatUpdateQueueEntry() {
                        UserID = player.UserID,
                        Faction = BarFaction.GetId(player.Faction),
                        Gamemode = match.Gamemode
                    });

                    await _UserRepository.Upsert(player.UserID, new BarUser() {
                        UserID = player.UserID,
                        Username = player.Name,
                        LastUpdated = match.StartTime,
                        CountryCode = player.CountryCode
                    }, cancel);

                    if (match.Gamemode != BarGamemode.DEFAULT && match.WrongSkillValues == false) {
                        await _UserSkillDb.Upsert(new BarUserSkill() {
                            UserID = player.UserID,
                            Gamemode = match.Gamemode,
                            Skill = player.Skill,
                            SkillUncertainty = player.SkillUncertainty,
                            LastUpdated = match.StartTime
                        }, cancel);
                    } else if (saidWrongSkillValues == false) {
                        _Logger.LogDebug($"not updating user skill due to wrong skill values [gameID={match.ID}]");
                        saidWrongSkillValues = true;
                    }
                } catch (Exception ex) {
                    _Logger.LogError(ex, $"failed to upsert user after parse [gameID={match.ID}] [userID={player.UserID}]");
                }
            }

            await _GameVersionUsageDb.Upsert(new GameVersionUsage() {
                Engine = match.Engine,
                Version = match.GameVersion,
                LastUsed = match.StartTime
            }, cancel);

        }

        /// <summary>
        ///     check if a start spot is valid for a specific match, where at least half of the players in the match
        ///     have a start spot position found from the start spot data
        /// </summary>
        /// <param name="match">match that contains the players to check the positions of</param>
        /// <param name="data">start spot data that contains the positions to ensure</param>
        /// <returns>
        ///     a tuple containing a bool and 2 ints. the bool indicates if the start spot data is valid for that
        ///     particular match, while the ints provide how many start spots were found to be valid, and how 
        ///     many were missing a valid start spot
        /// </returns>
        private (bool valid, int found, int missing) _CheckStartSpotValidity(BarMatch match, StartSpotData data) {
            int foundSpot = 0;
            int missingSpot = 0;
            foreach (BarMatchPlayer player in match.Players) {
                StartSpotSideStart? startSpot = data.GetNearestStartSpot(match.AllyTeams.Count, player.StartingPosition.X, player.StartingPosition.Z);
                if (startSpot != null) {
                    ++foundSpot;
                } else {
                    ++missingSpot;
                }
            }

            int playerCount = match.Players.Count;
            return (playerCount / 2 < foundSpot, foundSpot, missingSpot);
        }

    }
}
