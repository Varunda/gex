using gex.Models.Db;
using gex.Models.Queues;
using gex.Models.UserStats;
using gex.Services.Db.Match;
using gex.Services.Db.UserStats;
using gex.Services.Queues;
using gex.Services.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.QueueProcessor {

    public class UserMapStatUpdateQueueProcessor : BaseQueueProcessor<UserMapStatUpdateQueueEntry> {

        private readonly BarMatchRepository _MatchRepository;
        private readonly BarMatchAllyTeamDb _AllyTeamDb;
        private readonly BarMatchPlayerRepository _PlayerRepository;
        private readonly BarUserMapStatsDb _MapStatsDb;

        public UserMapStatUpdateQueueProcessor(ILoggerFactory factory,
            BaseQueue<UserMapStatUpdateQueueEntry> queue, ServiceHealthMonitor serviceHealthMonitor,
            BarMatchRepository matchRepository, BarUserMapStatsDb mapStatsDb,
            BarMatchAllyTeamDb allyTeamDb, BarMatchPlayerRepository playerRepository)

        : base("user_map_stats_update", factory, queue, serviceHealthMonitor) {

            _MatchRepository = matchRepository;
            _MapStatsDb = mapStatsDb;
            _AllyTeamDb = allyTeamDb;
            _PlayerRepository = playerRepository;
        }

        protected override async Task<bool> _ProcessQueueEntry(UserMapStatUpdateQueueEntry entry, CancellationToken cancel) {

            Stopwatch timer = Stopwatch.StartNew();
            _Logger.LogTrace($"updating user map stats [userID={entry.UserID}] [map={entry.Map}] [gamemode={entry.Gamemode}]");

            List<BarMatch> matches = await _MatchRepository.GetByUserID(entry.UserID, cancel);
            if (matches.Count == 0) {
                if (entry.MaybeNone == false) {
                    _Logger.LogWarning($"there are no matches for this user? [userID={entry.UserID}] [map={entry.Map}] [gamemode={entry.Gamemode}]");
                }
                return true;
            }

            matches = matches.Where(iter => iter.Map == entry.Map && iter.Gamemode == entry.Gamemode).ToList();
            if (matches.Count == 0) {
                if (entry.MaybeNone == false) {
                    _Logger.LogWarning($"apparently there are no matches to update map stats on [userID={entry.UserID}] [map={entry.Map}] [gamemode={entry.Gamemode}]");
                }
                return true;
            }

            BarUserMapStats stats = new();
            stats.UserID = entry.UserID;
            stats.Map = entry.Map;
            stats.Gamemode = entry.Gamemode;
            stats.PlayCount = matches.Count;

            // for each match, check if there was a winning ally team (else it was a tie), then check
            //      what ally team the player was on, and if they won or not
            foreach (BarMatch match in matches) {
                if (match.WrongSkillValues == true) {
                    continue;
                }

                List<BarMatchAllyTeam> allyTeams = await _AllyTeamDb.GetByGameID(match.ID, cancel);

                // tie check, if no teams won, then it's a tie. no need to load players then!
                if (allyTeams.FirstOrDefault(iter => iter.Won == true) == null) {
                    ++stats.TieCount;
                    continue;
                }

                List<BarMatchPlayer> players = await _PlayerRepository.GetByGameID(match.ID, cancel);
                BarMatchPlayer? player = players.FirstOrDefault(iter => iter.UserID == entry.UserID);

                if (player == null) {
                    _Logger.LogWarning($"missing player from game they played in! [userID={entry.UserID}] [gameID={match.ID}");
                    continue;
                }

                BarMatchAllyTeam? allyTeam = allyTeams.FirstOrDefault(iter => iter.AllyTeamID == player.AllyTeamID);
                if (allyTeam == null) {
                    _Logger.LogWarning($"missing ally team from a player [userID={entry.UserID}] [allyTeamID={player.AllyTeamID}] [gameID={match.ID}]");
                    continue;
                }

                if (allyTeam.Won == true) {
                    ++stats.WinCount;
                } else {
                    ++stats.LossCount;
                }
            }

            stats.LastUpdated = DateTime.UtcNow;
            bool doUpsert = stats.WinCount != 0 || stats.PlayCount != 0 || stats.LossCount != 0;
            if (doUpsert) {
                await _MapStatsDb.Upsert(stats, cancel);
            }

            _Logger.LogTrace($"updated user map stats [ignored={!doUpsert}] [userID={entry.UserID}] [map={entry.Map}] "
                + $"[gamemode={entry.Gamemode}] [timer={timer.ElapsedMilliseconds}ms]");

            return true;
        }
    }
}
