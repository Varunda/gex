using gex.Code.Constants;
using gex.Models;
using gex.Models.Bar;
using gex.Models.Db;
using gex.Models.Options;
using gex.Models.Queues;
using gex.Services.Db;
using gex.Services.Db.Match;
using gex.Services.Db.Readers;
using gex.Services.Db.UserStats;
using gex.Services.Demofile;
using gex.Services.Queues;
using gex.Services.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.QueueProcessor {

    public class GameReplayParseQueueProcessor : BaseQueueProcessor<GameReplayParseQueueEntry> {

        private readonly BarMatchRepository _MatchRepository;
        private readonly BarReplayDb _ReplayDb;
        private readonly BarMatchAllyTeamDb _MatchAllyTeamDb;
        private readonly BarMatchSpectatorDb _MatchSpectatorDb;
        private readonly BarMatchChatMessageDb _MatchChatMessageDb;
        private readonly BarMatchPlayerRepository _PlayerRepository;
        private readonly BarMapRepository _BarMapRepository;
        private readonly BarUserDb _UserDb;
        private readonly BarUserSkillDb _UserSkillDb;

        private readonly BarMatchProcessingDb _ProcessingDb;
        private readonly IOptions<FileStorageOptions> _Options;
        private readonly BarDemofileParser _Parser;
        private readonly BaseQueue<HeadlessRunQueueEntry> _HeadlessRunQueue;

        private readonly BaseQueue<UserMapStatUpdateQueueEntry> _MapStatUpdateQueue;
        private readonly BaseQueue<UserFactionStatUpdateQueueEntry> _FactionStatUpdateQueue;

        public GameReplayParseQueueProcessor(ILoggerFactory factory,
            BaseQueue<GameReplayParseQueueEntry> queue, ServiceHealthMonitor serviceHealthMonitor,
            BarMatchProcessingDb processingDb, IOptions<FileStorageOptions> options,
            BarDemofileParser parser, BaseQueue<HeadlessRunQueueEntry> headlessRunQueue,
            BarMatchRepository matchRepository, BarMatchAllyTeamDb matchAllyTeamDb,
            BarMatchSpectatorDb matchSpectatorDb, BarMatchChatMessageDb matchChatMessageDb,
            BarMatchPlayerRepository playerRepository, BarMapRepository barMapRepository,
            BarReplayDb replayDb, BarUserDb userDb,
            BarUserSkillDb userSkillDb, BaseQueue<UserMapStatUpdateQueueEntry> mapStatUpdateQueue,
            BaseQueue<UserFactionStatUpdateQueueEntry> factionStatUpdateQueue) :

        base("game_replay_parse_queue", factory, queue, serviceHealthMonitor) {

            _ProcessingDb = processingDb;
            _Options = options;
            _Parser = parser;
            _HeadlessRunQueue = headlessRunQueue;
            _MatchRepository = matchRepository;
            _MatchAllyTeamDb = matchAllyTeamDb;
            _MatchSpectatorDb = matchSpectatorDb;
            _MatchChatMessageDb = matchChatMessageDb;
            _PlayerRepository = playerRepository;
            _BarMapRepository = barMapRepository;
            _ReplayDb = replayDb;
            _UserDb = userDb;
            _UserSkillDb = userSkillDb;
            _MapStatUpdateQueue = mapStatUpdateQueue;
            _FactionStatUpdateQueue = factionStatUpdateQueue;
        }

        protected override async Task<bool> _ProcessQueueEntry(GameReplayParseQueueEntry entry, CancellationToken cancel) {

            _Logger.LogInformation($"parsing game replay [gameID={entry.GameID}]");
            Stopwatch timer = Stopwatch.StartNew();

            BarReplay? replay = await _ReplayDb.GetByID(entry.GameID);
            if (replay == null) {
                _Logger.LogError($"cannot parse replay file, replay is missing from DB [gameID={entry.GameID}]");
                return false;
            }

            Stopwatch stepTimer = Stopwatch.StartNew();

            bool runHeadless = entry.ForceForward;

            BarMatch? existingMatch = await _MatchRepository.GetByID(entry.GameID, cancel);
            if (existingMatch != null && entry.Force == false) {
                _Logger.LogInformation($"replay file already parsed [gameID={entry.GameID}]");

                List<BarMatchPlayer> players = await _PlayerRepository.GetByGameID(entry.GameID, cancel);
                runHeadless |= players.Count == 2;
            } else {
                if (entry.Force == true) {
                    Stopwatch delTimer = Stopwatch.StartNew();
                    _Logger.LogInformation($"deleting database data due to forced run [gameID={entry.GameID}]");
                    await _MatchAllyTeamDb.DeleteByGameID(entry.GameID);
                    await _PlayerRepository.DeleteByGameID(entry.GameID);
                    await _MatchSpectatorDb.DeleteByGameID(entry.GameID);
                    await _MatchChatMessageDb.DeleteByGameID(entry.GameID);
                    await _MatchRepository.Delete(entry.GameID);
                    _Logger.LogDebug($"deleting previous game data [gameID={entry.GameID}] [timer={delTimer.ElapsedMilliseconds}ms]");
                }

                string replayPath = Path.Join(_Options.Value.ReplayLocation, replay.FileName);
                if (File.Exists(replayPath) == false) {
                    _Logger.LogError($"missing replay file [gameID={entry.GameID}] [path={replayPath}]");
                    return false;
                }

                _Logger.LogDebug($"opening game replay [gameID={entry.GameID}] [path={replayPath}]");
                byte[] file = await File.ReadAllBytesAsync(replayPath, cancel);

                Result<BarMatch, string> match = await _Parser.Parse(replay.FileName, file, cancel);
                long parseReplayMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

                _Logger.LogDebug($"parsed replay file into match [ID={entry.GameID}]");

                if (match.IsOk == false) {
                    _Logger.LogError($"failed to parse replay data: {match.Error}");
                    return false;
                }

                BarMatch parsed = match.Value;
                parsed.MapName = replay.MapName;

                BarMap? map = await _BarMapRepository.GetByName(replay.MapName, cancel);
                if (map == null) {
                    _Logger.LogWarning($"missing bar map! [map={replay.MapName}] [gameID={parsed.ID}]");
                }

                foreach (BarMatchAllyTeam allyTeam in parsed.AllyTeams) {
                    await _MatchAllyTeamDb.Insert(allyTeam);
                }
                long insertAllyTeamsMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

                foreach (BarMatchPlayer player in parsed.Players) {
                    await _PlayerRepository.Insert(player);
                }
                long insertPlayersMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

                foreach (BarMatchSpectator spec in parsed.Spectators) {
                    await _MatchSpectatorDb.Insert(spec);
                }
                long insertSpecMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

                foreach (BarMatchChatMessage msg in parsed.ChatMessages) {
                    await _MatchChatMessageDb.Insert(msg);
                }
                long insertChatMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

                await _MatchRepository.Insert(parsed, cancel);
                long insertMatchMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

                _Logger.LogInformation($"processed match [ID={entry.GameID}] [parse replay={parseReplayMs}ms]"
                    + $" [ally team db={insertAllyTeamsMs}ms] [player db={insertPlayersMs}ms]"
                    + $" [spec ms={insertSpecMs}ms] [chat db={insertChatMs}ms] [match db={insertMatchMs}ms]");

                foreach (BarMatchPlayer player in parsed.Players) {
                    try {
                        _MapStatUpdateQueue.Queue(new UserMapStatUpdateQueueEntry() {
                            UserID = player.UserID,
                            Map = parsed.Map,
                            Gamemode = parsed.Gamemode
                        });

                        _FactionStatUpdateQueue.Queue(new UserFactionStatUpdateQueueEntry() {
                            UserID = player.UserID,
                            Faction = BarFaction.GetId(player.Faction),
                            Gamemode = parsed.Gamemode
                        });

                        await _UserDb.Upsert(player.UserID, new Models.UserStats.BarUser() {
                            UserID = player.UserID,
                            Username = player.Name,
                            LastUpdated = DateTime.UtcNow
                        }, cancel);

                        if (parsed.Gamemode != BarGamemode.DEFAULT) {
                            await _UserSkillDb.Upsert(new Models.UserStats.BarUserSkill() {
                                UserID = player.UserID,
                                Gamemode = parsed.Gamemode,
                                Skill = player.Skill,
                                SkillUncertainty = player.SkillUncertainty,
                                LastUpdated = DateTime.UtcNow
                            }, cancel);
                        }
                    } catch (Exception ex) {
                        _Logger.LogError(ex, $"failed to upsert user after parse [gameID={entry.GameID}] [userID={player.UserID}]");
                    }
                }

                runHeadless |= parsed.Players.Count == 2;
            }

            BarMatchProcessing processing = await _ProcessingDb.GetByGameID(entry.GameID, cancel)
                ?? throw new Exception($"missing expected {nameof(BarMatchProcessing)} {entry.GameID}");

            processing.ReplayParsed = DateTime.UtcNow;
            processing.ReplayParsedMs = (int)timer.ElapsedMilliseconds;
            await _ProcessingDb.Upsert(processing);

            if (runHeadless && (entry.ForceForward == true || processing.ReplaySimulated == null)) {
                _Logger.LogDebug($"putting entry into headless run queue [gameID={entry.GameID}]");
                _HeadlessRunQueue.Queue(new HeadlessRunQueueEntry() {
                    GameID = entry.GameID,
                    Force = entry.Force,
                    ForceForward = entry.ForceForward
                });
            }

            return true;
        }

    }
}
