using gex.Code;
using gex.Common.Code.ExtensionMethods;
using gex.Common.Models.Bar;
using gex.Common.Models.Match;
using gex.Common.Services.Repositories;
using gex.Common.Services.Repository.Match;
using gex.Common.Services.Storage;
using gex.Models;
using gex.Models.Db;
using gex.Models.Internal;
using gex.Models.Queues;
using gex.Services;
using gex.Services.Db;
using gex.Services.Queues;
using gex.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Controllers.Api {

    [ApiController]
    [Route("/api/gex-import")]
    public class GexImporterApiController : ApiControllerBase {

        private readonly ILogger<GexImporterApiController> _Logger;
        private readonly ICurrentAccount _CurrentUser;

        private readonly BarMatchRepository _MatchRepository;
        private readonly DemofileStorage _DemofileStorage;
        private readonly BarReplayDb _ReplayDb;
        private readonly BarMatchProcessingRepository _ProcessingRepository;
        private readonly BaseQueue<GameReplayParseQueueEntry> _ParseQueue;

        private readonly MatchPoolRepository _PoolRepository;
        private readonly MatchPoolEntryRepository _PoolEntryRepository;

        private static HttpClient _Http = new();

        static GexImporterApiController() {
            _Http.DefaultRequestHeaders.UserAgent.TryParseAdd("gex-importer/0.1");
        }

        public GexImporterApiController(ILogger<GexImporterApiController> logger,
            DemofileStorage demofileStorage, BaseQueue<GameReplayParseQueueEntry> parseQueue,
            BarMatchRepository matchRepository, BarMatchProcessingRepository processingRepository,
            BarReplayDb replayDb, MatchPoolRepository poolRepository,
            MatchPoolEntryRepository poolEntryRepository, ICurrentAccount currentUser) {

            _Logger = logger;

            _DemofileStorage = demofileStorage;
            _ParseQueue = parseQueue;
            _MatchRepository = matchRepository;
            _ProcessingRepository = processingRepository;
            _ReplayDb = replayDb;

            _PoolRepository = poolRepository;
            _PoolEntryRepository = poolEntryRepository;
            _CurrentUser = currentUser;
        }

        /// <summary>
        ///     import a <see cref="BarMatch"/> from a different gex instance
        /// </summary>
        /// <param name="gexUrl">URL of the gex instance. do not include a trailing slash</param>
        /// <param name="gameID">ID of the game to import</param>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///     the <see cref="BarMatch"/> with <see cref="BarMatch.ID"/> of <paramref name="gameID"/> was successfully
        ///     imported from the gex instance found at <paramref name="gexUrl"/>
        /// </response>
        /// <response code="400">
        ///     the gex instance at <paramref name="gexUrl"/> did not return a 200 OK for the <see cref="BarMatch"/>
        ///     with <see cref="BarMatch.ID"/> of <paramref name="gameID"/>
        /// </response>
        /// <response code="500">
        ///     
        /// </response>
        [HttpPost("match/{gameID}")]
        [PermissionNeeded(AppPermission.GEX_DEV)]
        [Authorize]
        public async Task<ApiResponse> ImportMatch([FromQuery] string gexUrl, string gameID, CancellationToken cancel) {
            Stopwatch timer = Stopwatch.StartNew();
            using HttpResponseMessage apiRes = await _Http.GetAsync($"{gexUrl}/api/match/{gameID}", cancel);
            long downloadMs = timer.ElapsedMilliseconds;

            using MemoryStream ms = new();
            await apiRes.Content.CopyToAsync(ms, cancel);

            string body = Encoding.UTF8.GetString(ms.ToArray());

            if (apiRes.StatusCode != HttpStatusCode.OK) {
                return ApiBadRequest(body);
            }

            JsonElement matchJson;
            try {
                matchJson = JsonSerializer.Deserialize<JsonElement>(body);
            } catch (Exception ex) {
                _Logger.LogError(ex, $"failed to parse json response from gex api [gameID={gameID}] [gex url={gexUrl}]");
                return ApiInternalError(ex);
            }

            string gexGameID = matchJson.GetRequiredString("id");
            if (gameID != gexGameID) {
                _Logger.LogError($"mismatched gameID from gex? [gameID={gameID}] [gex url={gexUrl}] [gex game id={gexGameID}]");
                return ApiInternalError($"mismatched gameID. expected input {gameID} to match gex game {gexGameID}");
            }
            string gexFilename = matchJson.GetRequiredString("fileName");

            BarMatch? match = await _MatchRepository.GetByID(gameID, cancel);
            if (match != null) {
                return ApiBadRequest($"{nameof(BarMatch)} {gameID} already exists in DB");
            }

            using HttpResponseMessage demofileRes = await _Http.GetAsync($"{gexUrl}/downloadmatch/{gameID}", cancel);
            using MemoryStream dfMs = new();
            await demofileRes.Content.CopyToAsync(dfMs, cancel);

            if (demofileRes.StatusCode != HttpStatusCode.OK) {
                return ApiBadRequest(Encoding.UTF8.GetString(dfMs.ToArray()));
            }

            _Logger.LogDebug($"writing imported demofile to storage [gameID={gameID}] [filename={gexFilename}]");

            await _DemofileStorage.Write(gexFilename, dfMs.ToArray(), false, CancellationToken.None);

            BarReplay replay = new();
            replay.ID = gameID;
            replay.FileName = gexFilename;
            replay.MapName = matchJson.GetRequiredString("mapName");
            await _ReplayDb.Insert(replay, CancellationToken.None);

            BarMatchProcessing processing = new();
            processing.GameID = gameID;
            processing.ReplayDownloaded = DateTime.UtcNow;
            processing.ReplayDownloadedMs = (int)downloadMs;
            await _ProcessingRepository.Upsert(processing);

            _ParseQueue.Queue(new GameReplayParseQueueEntry() {
                GameID = gameID,
            });

            _Logger.LogInformation($"successfully imported gex match [gameID={gameID}] [gex url={gexUrl}]");

            return ApiOk();
        }

        [HttpPost("pool/{poolID}")]
        [PermissionNeeded(AppPermission.GEX_DEV)]
        [Authorize]
        public async Task<ApiResponse> ImportPool([FromQuery] string gexUrl, long poolID, CancellationToken cancel) {

            using HttpResponseMessage apiRes = await _Http.GetAsync($"{gexUrl}/api/match-pool/{poolID}", cancel);
            using MemoryStream ms = new();
            await apiRes.Content.CopyToAsync(ms, cancel);
            string body = Encoding.UTF8.GetString(ms.ToArray());

            if (apiRes.StatusCode != HttpStatusCode.OK) {
                return ApiBadRequest(body);
            }

            MatchPool? pool = JsonSerializer.Deserialize<MatchPool>(body, new JsonSerializerOptions() {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (pool == null) {
                return ApiBadRequest($"pool was null after deserializing");
            }

            _Logger.LogDebug($"loaded pool [poolID={poolID}] [gex url={gexUrl}]");

            using HttpResponseMessage matchesRes = await _Http.GetAsync($"{gexUrl}/api/match-pool/{poolID}/entries", cancel);
            using MemoryStream mms = new();
            await matchesRes.Content.CopyToAsync(mms, cancel);
            string matchesBody = Encoding.UTF8.GetString(mms.ToArray());

            if (matchesRes.StatusCode != HttpStatusCode.OK) {
                return ApiBadRequest(matchesBody);
            }

            List<MatchPoolEntry>? entries = JsonSerializer.Deserialize<List<MatchPoolEntry>>(matchesBody, new JsonSerializerOptions() {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (entries == null) {
                return ApiBadRequest($"entries were null after deserializing");
            }

            AppAccount currentUser = await _CurrentUser.Get(cancel)
                ?? throw new InvalidOperationException($"expected a logged in user");

            pool.CreatedByID = currentUser.ID;

            long newPoolID = await _PoolRepository.Create(pool, CancellationToken.None);

            foreach (MatchPoolEntry entry in entries) {
                entry.PoolID = newPoolID;
                await _PoolEntryRepository.Insert(entry, CancellationToken.None);
            }

            return ApiOk();
        }

    }
}
