using gex.Code.Constants;
using gex.Models.Bar;
using gex.Models.Db;
using gex.Models.Queues;
using gex.Services.Db;
using gex.Services.Db.Match;
using gex.Services.Db.UserStats;
using gex.Services.Queues;
using gex.Services.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
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
        private readonly BarUserDb _UserDb;
        private readonly BarUserSkillDb _UserSkillDb;
        private readonly GameVersionUsageDb _GameVersionUsageDb;
        private readonly MapPriorityModDb _MapPriorityModDb;
        private readonly BarMatchPriorityCalculator _PriorityCalculator;
        private readonly BarMatchTeamDeathDb _TeamDeathDb;

        private readonly BaseQueue<HeadlessRunQueueEntry> _HeadlessRunQueue;
        private readonly BaseQueue<UserMapStatUpdateQueueEntry> _MapStatUpdateQueue;
        private readonly BaseQueue<UserFactionStatUpdateQueueEntry> _FactionStatUpdateQueue;

        public BarDemofileResultProcessor(ILogger<BarDemofileResultProcessor> logger,
            BarMatchRepository matchRepository, BarReplayDb replayDb,
            BarMatchAllyTeamDb matchAllyTeamDb, BarMatchSpectatorDb matchSpectatorDb,
            BarMatchChatMessageDb matchChatMessageDb, BarMatchPlayerRepository playerRepository,
            BarMapRepository barMapRepository, BarUserDb userDb,
            BarUserSkillDb userSkillDb, GameVersionUsageDb gameVersionUsageDb,
            MapPriorityModDb mapPriorityModDb, BarMatchPriorityCalculator priorityCalculator,
            BaseQueue<HeadlessRunQueueEntry> headlessRunQueue, BaseQueue<UserMapStatUpdateQueueEntry> mapStatUpdateQueue,
            BaseQueue<UserFactionStatUpdateQueueEntry> factionStatUpdateQueue, BarMatchTeamDeathDb teamDeathDb) {

            _Logger = logger;

            _MatchRepository = matchRepository;
            _ReplayDb = replayDb;
            _MatchAllyTeamDb = matchAllyTeamDb;
            _MatchSpectatorDb = matchSpectatorDb;
            _MatchChatMessageDb = matchChatMessageDb;
            _PlayerRepository = playerRepository;
            _BarMapRepository = barMapRepository;
            _UserDb = userDb;
            _UserSkillDb = userSkillDb;
            _GameVersionUsageDb = gameVersionUsageDb;
            _MapPriorityModDb = mapPriorityModDb;
            _PriorityCalculator = priorityCalculator;
            _HeadlessRunQueue = headlessRunQueue;
            _MapStatUpdateQueue = mapStatUpdateQueue;
            _FactionStatUpdateQueue = factionStatUpdateQueue;
            _TeamDeathDb = teamDeathDb;
        }

        public async Task Process(BarMatch match, CancellationToken cancel) {

            BarMap? map = await _BarMapRepository.GetByFileName(match.MapName, cancel);
            if (map == null) {
                _Logger.LogWarning($"missing bar map! [map={match.MapName}] [gameID={match.ID}]");
            }

            Stopwatch stepTimer = Stopwatch.StartNew();
            foreach (BarMatchAllyTeam allyTeam in match.AllyTeams) {
                await _MatchAllyTeamDb.Insert(allyTeam);
            }
            long insertAllyTeamsMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

            foreach (BarMatchPlayer player in match.Players) {
                await _PlayerRepository.Insert(player);
            }
            long insertPlayersMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

            foreach (BarMatchSpectator spec in match.Spectators) {
                await _MatchSpectatorDb.Insert(spec);
            }
            long insertSpecMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

            foreach (BarMatchChatMessage msg in match.ChatMessages) {
                await _MatchChatMessageDb.Insert(msg);
            }
            long insertChatMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

            foreach (BarMatchTeamDeath death in match.TeamDeaths) {
                await _TeamDeathDb.Insert(death, cancel);
            }
            long insertDeathMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

            await _MatchRepository.Insert(match, cancel);
            long insertMatchMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

            _Logger.LogInformation($"processed match [ID={match.ID}]"
                + $" [ally team db={insertAllyTeamsMs}ms] [player db={insertPlayersMs}ms]"
                + $" [spec ms={insertSpecMs}ms] [chat db={insertChatMs}ms] [death db={match.TeamDeaths.Count}/{insertDeathMs}ms] [match db={insertMatchMs}ms]");

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

                    await _UserDb.Upsert(player.UserID, new Models.UserStats.BarUser() {
                        UserID = player.UserID,
                        Username = player.Name,
                        LastUpdated = DateTime.UtcNow
                    }, cancel);

                    if (match.Gamemode != BarGamemode.DEFAULT) {
                        await _UserSkillDb.Upsert(new Models.UserStats.BarUserSkill() {
                            UserID = player.UserID,
                            Gamemode = match.Gamemode,
                            Skill = player.Skill,
                            SkillUncertainty = player.SkillUncertainty,
                            LastUpdated = DateTime.UtcNow
                        }, cancel);
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

    }
}
