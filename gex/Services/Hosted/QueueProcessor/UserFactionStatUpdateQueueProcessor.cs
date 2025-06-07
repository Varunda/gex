using gex.Code.Constants;
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

    public class UserFactionStatUpdateQueueProcessor : BaseQueueProcessor<UserFactionStatUpdateQueueEntry> {

        private readonly BarMatchRepository _MatchRepository;
        private readonly BarMatchAllyTeamDb _AllyTeamDb;
        private readonly BarMatchPlayerRepository _PlayerRepository;
        private readonly BarUserFactionStatsDb _FactionStatsDb;

        public UserFactionStatUpdateQueueProcessor(ILoggerFactory factory,
            BaseQueue<UserFactionStatUpdateQueueEntry> queue, ServiceHealthMonitor serviceHealthMonitor,
            BarMatchRepository matchRepository, BarMatchAllyTeamDb allyTeamDb,
            BarMatchPlayerRepository playerRepository, BarUserFactionStatsDb factionStatsDb)

        : base("user_faction_stat_update_queue", factory, queue, serviceHealthMonitor) {

            _MatchRepository = matchRepository;
            _AllyTeamDb = allyTeamDb;
            _PlayerRepository = playerRepository;
            _FactionStatsDb = factionStatsDb;
        }

        protected override async Task<bool> _ProcessQueueEntry(UserFactionStatUpdateQueueEntry entry, CancellationToken cancel) {
            Stopwatch timer = Stopwatch.StartNew();
            string facName = BarFaction.GetName(entry.Faction);
            _Logger.LogTrace($"updating user faction stats [userID={entry.UserID}] [faction={entry.Faction}/{facName}] [gamemode={entry.Gamemode}]");

            List<BarMatch> matches = await _MatchRepository.GetByUserID(entry.UserID, cancel);
            if (matches.Count == 0) {
                _Logger.LogWarning($"there are no matches for this user? [userID={entry.UserID}] [faction={entry.Faction}]");
                return false;
            }

            matches = matches.Where(iter => iter.Gamemode == entry.Gamemode).ToList();
            if (matches.Count == 0) {
                _Logger.LogWarning($"there are no matches for this gamemode? [userID={entry.UserID}] [faction={entry.Faction}] [gamemode={entry.Gamemode}]");
            }

            BarUserFactionStats stats = new();
            stats.UserID = entry.UserID;
            stats.Faction = entry.Faction;
            stats.Gamemode = entry.Gamemode;

            // for each match, check if there was a winning ally team (else it was a tie), then check
            //      what ally team the player was on, and if they won or not
            foreach (BarMatch match in matches) {
                List<BarMatchPlayer> players = await _PlayerRepository.GetByGameID(match.ID, cancel);
                BarMatchPlayer? player = players.FirstOrDefault(iter => iter.UserID == entry.UserID);

                if (player == null) {
                    _Logger.LogWarning($"missing player from game they played in! [userID={entry.UserID}] [gameID={match.ID}");
                    continue;
                }

                if (player.Faction != facName) {
                    continue;
                }

				stats.PlayCount += 1;

                List<BarMatchAllyTeam> allyTeams = await _AllyTeamDb.GetByGameID(match.ID, cancel);
                if (allyTeams.FirstOrDefault(iter => iter.Won == true) == null) {
                    ++stats.TieCount;
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
            await _FactionStatsDb.Upsert(stats, cancel);

            _Logger.LogTrace($"updated user faction stats [userID={entry.UserID}] [faction={entry.Faction}/{facName}] "
                + $"[gamemode={entry.Gamemode}] [timer={timer.ElapsedMilliseconds}ms]");

            return true;
        }
    }
}
