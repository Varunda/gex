using gex.Common.Code.Constants;
using gex.Common.Models;
using gex.Models;
using gex.Models.Db;
using gex.Services.Db;
using gex.Services.Db.Account;
using gex.Services.Db.Match;
using gex.Services.Migrations;
using gex.Services.Parser;
using gex.Services.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

    public class BarMatchRepository {

        private readonly ILogger<BarMatchRepository> _Logger;
        private readonly BarMatchDb _MatchDb;

        private readonly BarMatchAllyTeamDb _AllyTeamDb;
        private readonly BarMatchChatMessageDb _ChatMessageDb;
        private readonly BarMatchSpectatorDb _SpectatorDb;
        private readonly BarMatchPlayerLeftDb _PlayerLeftDb;
        private readonly BarMatchTeamDeathDb _TeamDeathDb;
        private readonly BarMatchPlayerRepository _PlayerRepository;
        private readonly BarMatchProcessingRepository _ProcessingRepository;
        private readonly BarDemofileParser _DemofileParser;
        private readonly DemofileStorage _DemofileStorage;
        private readonly MatchPoolRepository _MatchPoolRepository;
        private readonly MatchPoolEntryDb _MatchPoolEntryDb;
        private readonly BarMatchTextPingDb _TextPingDb;

        private readonly IMemoryCache _Cache;
        private const string CACHE_KEY_ID = "Gex.Match.ID.{0}"; // {0} => game ID
        private const string CACHE_KEY_OLDEST = "Gex.Match.Oldest";
        private const string CACHE_KEY_UNIQUE_ENGINES = "Gex.Match.Unique.Engines";
        private const string CACHE_KEY_UNIQUE_GAME_VERSIONS = "Gex.Match.Unique.GameVersions";
        private const string CACHE_KEY_GAMES_BY_USER = "Gex.Match.User.{0}"; // {0} => user ID

        public BarMatchRepository(ILogger<BarMatchRepository> logger,
            BarMatchDb matchDb, IMemoryCache cache,
            BarMatchAllyTeamDb allyTeamDb, BarMatchChatMessageDb chatMessageDb,
            BarMatchSpectatorDb spectatorDb, BarMatchPlayerLeftDb playerLeftDb,
            BarMatchTeamDeathDb teamDeathDb, BarMatchPlayerRepository playerRepository, 
            BarMatchProcessingRepository processingRepository, BarDemofileParser demofileParser,
            DemofileStorage demofileStorage, MatchPoolRepository matchPoolRepository,
            MatchPoolEntryDb matchPoolEntryDb, BarMatchTextPingDb textPingDb) { 

            _Logger = logger;
            _MatchDb = matchDb;
            _Cache = cache;
            _AllyTeamDb = allyTeamDb;
            _ChatMessageDb = chatMessageDb;
            _SpectatorDb = spectatorDb;
            _PlayerLeftDb = playerLeftDb;
            _TeamDeathDb = teamDeathDb;
            _PlayerRepository = playerRepository;
            _ProcessingRepository = processingRepository;
            _DemofileParser = demofileParser;
            _DemofileStorage = demofileStorage;
            _MatchPoolRepository = matchPoolRepository;
            _MatchPoolEntryDb = matchPoolEntryDb;
            _TextPingDb = textPingDb;
        }

        public async Task<BarMatch?> GetByID(string gameID, CancellationToken cancel) {
            string cacheKey = string.Format(CACHE_KEY_ID, gameID);

            if (_Cache.TryGetValue(cacheKey, out BarMatch? match) == false) {
                match = await _MatchDb.GetByID(gameID, cancel);

                _Cache.Set(cacheKey, match, new MemoryCacheEntryOptions() {
                    SlidingExpiration = TimeSpan.FromMinutes(5)
                });
            }

            return match;
        }

        /// <summary>
        ///     build a <see cref="BarMatch"/>, loading all the options a user might be interested in
        /// </summary>
        /// <param name="gameID"></param>
        /// <param name="options"></param>
        /// <param name="currentUser"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<Result<BarMatch?, string>> BuildMatch(string gameID,
            BuildOptions options, AppAccount? currentUser,
            CancellationToken cancel
        ) {
            BarMatch? match = await GetByID(gameID, cancel);
            if (match == null) {
                return Result<BarMatch?, string>.Ok(null);
            }

            BarMatchProcessing? proc = await _ProcessingRepository.GetByGameID(gameID, cancel);
            if (proc == null) {
                Debug.Fail($"missing {nameof(BarMatchProcessing)} for gameID {gameID}");
                _Logger.LogError($"missing bar match processing [gameID={gameID}]");
            }

            List<MatchPoolEntry> poolEntries = await _MatchPoolEntryDb.GetByMatchID(gameID, cancel);
            if (poolEntries.Count > 0) {
                match.MatchPoolIsHidden = true;

                bool canView = false;
                foreach (MatchPoolEntry entry in poolEntries) {
                    canView |= await _MatchPoolRepository.CanView(entry.PoolID, currentUser?.ID, cancel);
                    if (canView == true) {
                        MatchPool allowedPool = await _MatchPoolRepository.GetByID(entry.PoolID, cancel)
                            ?? throw new Exception($"failsafe tripped, if canView is true, then how is this pool null?");
                        if (allowedPool.HideUntil != null) {
                            match.MatchPoolIsHidden = DateTime.UtcNow > allowedPool.HideUntil;
                        }
                        break;
                    }
                }

                if (canView == false) {
                    return "no permission to view this match";
                }
            }

            if (options.IncludeAllyTeams == true) {
                match.AllyTeams = await _AllyTeamDb.GetByGameID(gameID, cancel);
            }

            if (options.IncludePlayers == true) {
                match.Players = await _PlayerRepository.GetByGameID(gameID, cancel);
            }

            if (options.IncludeChat == true) {
                match.ChatMessages = await _ChatMessageDb.GetByGameID(gameID, cancel);
            }

            if (options.IncludeSpectators == true) {
                match.Spectators = await _SpectatorDb.GetByGameID(gameID, cancel);
            }

            if (options.IncludeTeamDeaths == true) {
                match.TeamDeaths = await _TeamDeathDb.GetByGameID(gameID, cancel);
            }

            // if the request wants player leaves, but the match feature's don't include it,
            //      build those here
            if (options.IncludePlayerLeaves == true) {
                if (proc != null && proc.Features.Contains("player_left") == false) {
                    _Logger.LogDebug($"match is missing player_left feature, fixing [gameID={gameID}]");

                    Result<BarMatch, string> fromDemofile = await _Parse(match, new DemofileParserOptions(), cancel);
                    if (fromDemofile.IsOk == false) {
                        _Logger.LogError($"failed to parse demofile [error={fromDemofile.Error}] [matchID={match.ID}]");
                        return $"failed to parse demofile for match [error={fromDemofile.Error}]";
                    }

                    await _PlayerLeftDb.DeleteByGameID(gameID);
                    foreach (BarMatchPlayerLeft left in fromDemofile.Value.PlayerLeaves) {
                        await _PlayerLeftDb.Insert(left, cancel);
                    }

                    proc.Features.Add("player_left");
                    await _ProcessingRepository.Upsert(proc);

                    match.PlayerLeaves = fromDemofile.Value.PlayerLeaves;
                } else {
                    match.PlayerLeaves = await _PlayerLeftDb.GetByGameID(gameID, cancel);
                }
            }

            // if the request wants labeled pings, but the match feature's don't include it,
            //      build those here
            if (options.IncludeLabeledPings == true && options.IncludeMapDraws == false) {
                if (proc != null && proc.Features.Contains("text_ping") == false) {
                    _Logger.LogDebug($"match is missing text_ping feature, fixing [gameID={gameID}]");

                    Result<BarMatch, string> fromDemofile = await _Parse(match, new DemofileParserOptions() {
                        IncludeMapDraws = true,
                    }, cancel);

                    if (fromDemofile.IsOk == false) {
                        _Logger.LogError($"failed to parse demofile [error={fromDemofile.Error}] [matchID={match.ID}]");
                        return $"failed to parse demofile for match [error={fromDemofile.Error}]";
                    }

                    await _TextPingDb.DeleteByGameID(gameID);
                    foreach (BarMatchMapDraw draw in fromDemofile.Value.MapDraws) {
                        if (draw.Action != "point" || draw is not BarMatchMapDrawPoint point || point.Label == "") {
                            continue;
                        }

                        if (match.MapDraws.FirstOrDefault(iter => {
                            return iter.GameTime == point.GameTime && iter.PlayerID == point.PlayerID 
                                && iter.Index == point.Index && iter.X == point.X && iter.Z == point.Z;
                        }) != null) {
                            _Logger.LogWarning($"duplicate map draw point found [gameID={gameID}] [gameTime={point.GameTime}] "
                                + $"[player={point.PlayerID}] [coords={point.X},{point.Z}]");
                            Debug.Fail("duplicate map draw point found");
                            continue;
                        }

                        point.GameID = match.ID;
                        await _TextPingDb.Insert(point, cancel);
                        match.MapDraws.Add(draw);
                    }

                    proc.Features.Add("text_ping");
                    await _ProcessingRepository.Upsert(proc);
                } else {
                    match.MapDraws.AddRange(await _TextPingDb.GetByGameID(gameID, cancel));
                }
            }

            // for any option that isn't stored in a db (and isn't stored in db if the feature is missing)
            //      get it from the demofile itself
            if (options.IncludeMapDraws == true || options.IncludeCommands == true || options.IncludeSelfDCommands == true) {
                Result<byte[], string> demofile = await _DemofileStorage.GetDemofileByFilename(match.FileName, cancel);
                if (demofile.IsOk == false) {
                    _Logger.LogError($"failed to load demofile from storage [error={demofile.Error}] [filename={match.FileName}]");
                    return $"failed to load demofile from storage [error={demofile.Error}]";
                }

                Result<BarMatch, string> fromDemofile = await _DemofileParser.Parse(match.FileName, demofile.Value, new DemofileParserOptions() {
                    IncludeCommands = options.IncludeCommands || options.IncludeSelfDCommands,
                    IncludeMapDraws = options.IncludeMapDraws,
                }, cancel);
                if (fromDemofile.IsOk == false) {
                    _Logger.LogError($"failed to parse demofile [error={fromDemofile.Error}] [matchID={match.ID}]");
                    return $"failed to parse demofile for match [error={fromDemofile.Error}]";
                }

                if (options.IncludeMapDraws == true) {
                    match.MapDraws = fromDemofile.Value.MapDraws;
                }

                if (options.IncludeLabeledPings == true && options.IncludeMapDraws == false) {
                    match.Commands = fromDemofile.Value.Commands.Where(iter => {
                        return iter.ID == BarCommandId.SELFD;
                    }).ToList();
                } else {
                    match.Commands = fromDemofile.Value.Commands;
                }
            }

            return match;
        }

        /// <summary>
        ///     load a list of <see cref="BarMatch"/>s
        /// </summary>
        /// <param name="IDs">list of IDs to load the matches of</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        public async Task<List<BarMatch>> GetByIDs(IEnumerable<string> IDs, CancellationToken cancel) {
            if (!IDs.Any()) {
                return new List<BarMatch>();
            }

            List<BarMatch> matches = [];

            List<string> localIDs = new(IDs);

            Stopwatch timer = Stopwatch.StartNew();
            // the .toList here is to make a copy, so its safe to remove entries from localIDs
            foreach (string ID in localIDs.ToList()) {
                cancel.ThrowIfCancellationRequested();

                string cacheKey = string.Format(CACHE_KEY_ID, ID);
                if (_Cache.TryGetValue(cacheKey, out BarMatch? match) == true && match != null) {
                    localIDs.Remove(ID);
                    matches.Add(match);
                }
            }
            long cacheMs = timer.ElapsedMilliseconds; timer.Restart();

            if (localIDs.Count > 0) {
                List<BarMatch> dbMatches = await _MatchDb.GetByIDs(localIDs, cancel);
                matches.AddRange(dbMatches);
            }

            long dbMs = timer.ElapsedMilliseconds; timer.Restart();

            foreach (BarMatch m in matches) {
                string cacheKey = string.Format(CACHE_KEY_ID, m.ID);
                _Cache.Set(cacheKey, m, new MemoryCacheEntryOptions() {
                    SlidingExpiration = TimeSpan.FromMinutes(5)
                });
            }

            _Logger.LogTrace($"loaded matches by ids [count={IDs.Count()}] [cache={cacheMs}ms] [db={dbMs}ms]");

            return matches;
        }

        public Task<List<BarMatch>> GetAll(CancellationToken cancel) {
            return _MatchDb.GetAll(cancel);
        }

        public Task<List<BarMatch>> Search(BarMatchSearchParameters parms, int offset, int limit, AppAccount? currentUser, CancellationToken cancel) {
            return _MatchDb.Search(parms, offset, limit, currentUser, cancel);
        }

        /// <summary>
        ///     get all bar matches a user has played in, including any matches that would
        ///     be hidden due to match pools
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<List<BarMatch>> GetByUserID(long userID, CancellationToken cancel) {
            // caching here is mostly used to speed up full user stat fixes
            string cacheKey = string.Format(CACHE_KEY_GAMES_BY_USER, userID);
            if (_Cache.TryGetValue(cacheKey, out List<BarMatch>? matches) == false || matches == null) {
                matches = await _MatchDb.GetByUserID(userID, cancel);

                _Cache.Set(cacheKey, matches, new MemoryCacheEntryOptions() {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
                });
            }

            return matches;
        }

        /// <summary>
        ///     get the oldest match that Gex has stored
        /// </summary>
        public async Task<BarMatch?> GetOldestMatch(CancellationToken cancel) {
            if (_Cache.TryGetValue(CACHE_KEY_OLDEST, out BarMatch? oldest) == false) {
                oldest = await _MatchDb.GetOldestMatch(cancel);

                // cache for a day if found, otherwise just 10 seconds
                _Cache.Set(CACHE_KEY_OLDEST, oldest, new MemoryCacheEntryOptions() {
                    AbsoluteExpirationRelativeToNow = oldest == null ? TimeSpan.FromSeconds(10) : TimeSpan.FromDays(1)
                });
            }

            return oldest;
        }

        /// <summary>
        ///     get a list of unique engines across all stored matches
        /// </summary>
        public async Task<List<string>> GetUniqueEngines(CancellationToken cancel) {
            if (_Cache.TryGetValue(CACHE_KEY_UNIQUE_ENGINES, out List<string>? list) == false || list == null) {
                list = await _MatchDb.GetUniqueEngines(cancel);

                // TODO: inserting this can probably invalidate this cached value
                _Cache.Set(CACHE_KEY_UNIQUE_ENGINES, list, new MemoryCacheEntryOptions() {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                });
            }

            return list;
        }

        /// <summary>
        ///     get a list of all unique game versions across all stored matches
        /// </summary>
        public async Task<List<string>> GetUniqueGameVersions(CancellationToken cancel) {
            if (_Cache.TryGetValue(CACHE_KEY_UNIQUE_GAME_VERSIONS, out List<string>? list) == false || list == null) {
                list = await _MatchDb.GetUniqueGameVersions(cancel);

                // TODO: inserting this can probably invalidate this cached value
                _Cache.Set(CACHE_KEY_UNIQUE_GAME_VERSIONS, list, new MemoryCacheEntryOptions() {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                });
            }

            return list;
        }

        public Task Insert(BarMatch match, CancellationToken cancel) {
            string cacheKey = string.Format(CACHE_KEY_ID, match.ID);
            _Cache.Remove(cacheKey);
            return _MatchDb.Insert(match, cancel);
        }

        public Task Delete(string gameID) {
            string cacheKey = string.Format(CACHE_KEY_ID, gameID);
            _Cache.Remove(cacheKey);
            return _MatchDb.Delete(gameID);
        }

        public class BuildOptions {
            public bool IncludeAllyTeams { get; set; } = false;
            public bool IncludePlayers { get; set; } = false;
            public bool IncludeChat { get; set; } = false;
            public bool IncludeSpectators { get; set; } = false;
            public bool IncludeTeamDeaths { get; set; } = false;
            public bool IncludePlayerLeaves { get; set; } = false;
            public bool IncludeMapDraws { get; set; } = false;
            public bool IncludeLabeledPings { get; set; } = false;
            public bool IncludeCommands { get; set; } = false;
            public bool IncludeSelfDCommands { get; set; } = false;
        }

        private async Task<Result<BarMatch, string>> _Parse(BarMatch match, DemofileParserOptions options, CancellationToken cancel) {
            Result<byte[], string> demofile = await _DemofileStorage.GetDemofileByFilename(match.FileName, cancel);
            if (demofile.IsOk == false) {
                _Logger.LogError($"failed to load demofile from storage [error={demofile.Error}] [filename={match.FileName}]");
                return demofile.Error;
            }

            Result<BarMatch, string> fromDemofile = await _DemofileParser.Parse(match.FileName, demofile.Value, options, cancel);
            if (fromDemofile.IsOk == false) {
                _Logger.LogError($"failed to parse demofile [error={demofile.Error}] [matchID={match.ID}]");
                return fromDemofile.Error;
            }

            return fromDemofile;
        }

    }
}
