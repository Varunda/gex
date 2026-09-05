using gex.Common.Code.Constants;
using gex.Common.Models;
using gex.Common.Models.Map;
using gex.Common.Models.Match;
using gex.Common.Services.Db.Match;
using gex.Common.Services.Parser;
using gex.Common.Services.Repositories;
using gex.Common.Services.Repository.Match;
using gex.Common.Services.Storage;
using gex.Common.Services.Util;
using gex.Models.Db;
using gex.Services.Db;
using gex.Services.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Util {

    public class GexBarMatchBuilderUtil : IBarMatchBuilderUtil {

        private readonly ILogger<GexBarMatchBuilderUtil> _Logger;

        private readonly BarMatchRepository _MatchRepository;
        private readonly IBarMatchTeamDb _TeamDb;
        private readonly IBarMatchAllyTeamDb _AllyTeamDb;
        private readonly IBarMatchChatMessageDb _ChatMessageDb;
        private readonly IBarMatchSpectatorDb _SpectatorDb;
        private readonly IBarMatchPlayerLeftDb _PlayerLeftDb;
        private readonly IBarMatchTeamDeathDb _TeamDeathDb;
        private readonly BarMatchPlayerRepository _PlayerRepository;
        private readonly BarMatchProcessingRepository _ProcessingRepository;
        private readonly BarDemofileParser _DemofileParser;
        private readonly DemofileStorage _DemofileStorage;
        private readonly MatchPoolRepository _MatchPoolRepository;
        private readonly MatchPoolEntryDb _MatchPoolEntryDb;
        private readonly IBarMatchTextPingDb _TextPingDb;
        private readonly PolygonStartboxUtil _PolygonStartboxUtil;

        public GexBarMatchBuilderUtil(ILogger<GexBarMatchBuilderUtil> logger,
            BarMatchRepository matchRepository, IBarMatchTeamDb teamDb,
            IBarMatchAllyTeamDb allyTeamDb, IBarMatchChatMessageDb chatMessageDb,
            IBarMatchSpectatorDb spectatorDb, IBarMatchPlayerLeftDb playerLeftDb,
            IBarMatchTeamDeathDb teamDeathDb, BarMatchPlayerRepository playerRepository,
            BarMatchProcessingRepository processingRepository, BarDemofileParser demofileParser,
            DemofileStorage demofileStorage, MatchPoolRepository matchPoolRepository,
            MatchPoolEntryDb matchPoolEntryDb, IBarMatchTextPingDb textPingDb,
            PolygonStartboxUtil polygonStartboxUtil) {

            _Logger = logger;
            _MatchRepository = matchRepository;
            _TeamDb = teamDb;
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
            _PolygonStartboxUtil = polygonStartboxUtil;
        }

        public async Task<Result<Maybe<BarMatch>, string>> BuildMatch(string gameID,
            IBarMatchBuilderUtil.BuildOptions options, long? currentUserID,
            CancellationToken cancel
        ) {

            BarMatch? match = await _MatchRepository.GetByID(gameID, cancel);
            if (match == null) {
                return Maybe<BarMatch>.None();
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
                    canView |= await _MatchPoolRepository.CanView(entry.PoolID, currentUserID, cancel);
                    if (canView == true) {
                        MatchPool allowedPool = await _MatchPoolRepository.GetByID(entry.PoolID, cancel)
                            ?? throw new Exception($"failsafe tripped, if canView is true, then how is this pool null?");
                        if (allowedPool.HideUntil != null) {
                            match.MatchPoolIsHidden = DateTime.UtcNow < allowedPool.HideUntil;
                        }
                        break;
                    }
                }

                if (canView == false) {
                    return "no permission to view this match";
                }
            }

            if (proc != null && proc.Features.Contains("teams") == false) {
                _Logger.LogDebug($"match is missing teams feature, fixing [gameID={gameID}]");
                Result<BarMatch, string> fromDemofile = await _Parse(match, new DemofileParserOptions(), cancel);
                if (fromDemofile.IsOk == false) {
                    _Logger.LogError($"failed to parse demofile [error={fromDemofile.Error}] [matchID={match.ID}]");
                    Debug.Fail("failed to parse demofile");
                    return $"failed to parse demofile for match [error={fromDemofile.Error}]";
                }

                foreach (BarMatchTeam team in fromDemofile.Value.Teams) {
                    await _TeamDb.Insert(team, cancel);
                }
            }

            if (options.IncludeTeams == true) {
                match.Teams = await _TeamDb.GetByGameID(gameID, cancel);
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
                        Debug.Fail("failed to parse demofile");
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
                        Debug.Fail("failed to parse demofile");
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
                    match.MapDraws = [];
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
                    Debug.Fail("failed to parse demofile");
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

            // start region data
            if (options.IncludeStartRegionData == true) {
                Result<Maybe<PolygonStartbox>, string> region = _PolygonStartboxUtil.GetFromMatch(match);
                if (region.IsOk == false) {
                    _Logger.LogError($"failed to parse polygon start region [gameID={match.ID}] [error={region.Error}]");
                } else if (region.Value.Has() == true) {
                    match.StartRegionData = new List<StartRegionData>();

                    PolygonStartbox boxes = region.Value.Get();

                    foreach (PolygonStartbox.Side side in boxes.Sides) {
                        List<PolygonStartboxUtil.Pair> verts = _PolygonStartboxUtil.TessellateRing(side.Anchors);

                        StartRegionData startRegion = new();
                        startRegion.AllyTeamID = side.Index;
                        startRegion.Regions = [ new StartRegion() {
                            Vertices = verts
                        }];

                        match.StartRegionData.Add(startRegion);
                    }
                }
            }

            return Maybe<BarMatch>.Some(match);
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
