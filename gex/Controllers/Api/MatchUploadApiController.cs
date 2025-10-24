using gex.Code;
using gex.Common.Models;
using gex.Models;
using gex.Models.Bar;
using gex.Models.Db;
using gex.Models.Internal;
using gex.Models.Options;
using gex.Models.Queues;
using gex.Services;
using gex.Services.Db;
using gex.Services.Parser;
using gex.Services.Queues;
using gex.Services.Repositories;
using gex.Services.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Controllers.Api {

    [ApiController]
    [Route("/api/match-upload")]
    public class MatchUploadApiController : ApiControllerBase {

        private readonly ILogger<MatchUploadApiController> _Logger;
        private readonly BarMatchRepository _MatchRepository;
        private readonly BarDemofileParser _DemofileParser;
        private readonly BarMatchProcessingRepository _ProcessingRepository;
        private readonly IOptions<FileStorageOptions> _Options;
        private readonly BarReplayDb _ReplayDb;
        private readonly BaseQueue<GameReplayParseQueueEntry> _ParseQueue;
        private readonly BarDemofileResultProcessor _DemofileProcessor;
        private readonly BarMapRepository _MapRepository;
        private readonly BarMatchPriorityCalculator _PriorityCalculator;
        private readonly BaseQueue<HeadlessRunQueueEntry> _RunQueue;
        private readonly ICurrentAccount _CurrentAccount;

        public MatchUploadApiController(ILogger<MatchUploadApiController> logger,
            BarMatchRepository matchRepository, BarDemofileParser demofileParser,
            BarMatchProcessingRepository processingRepository, IOptions<FileStorageOptions> options,
            BaseQueue<GameReplayParseQueueEntry> parseQueue, BarReplayDb replayDb,
            BarDemofileResultProcessor demofileProcessor, BarMapRepository mapRepository,
            BarMatchPriorityCalculator priorityCalculator, BaseQueue<HeadlessRunQueueEntry> runQueue,
            ICurrentAccount currentAccount) {

            _Logger = logger;
            _MatchRepository = matchRepository;
            _DemofileParser = demofileParser;
            _ProcessingRepository = processingRepository;
            _Options = options;
            _ParseQueue = parseQueue;
            _ReplayDb = replayDb;
            _DemofileProcessor = demofileProcessor;
            _MapRepository = mapRepository;
            _PriorityCalculator = priorityCalculator;
            _RunQueue = runQueue;
            _CurrentAccount = currentAccount;
        }

        [HttpPost("upload")]
        [RequestTimeout(1000 * 60)] // allow 60 secs to upload
        [DisableFormValueModelBinding]
        [Authorize]
        [PermissionNeeded(AppPermission.GEX_MATCH_UPLOAD)]
        public async Task<ApiResponse<BarMatch>> Upload(CancellationToken cancel) {
            AppAccount? currentUser = await _CurrentAccount.Get(cancel);
            if (currentUser == null) {
                return ApiInternalError<BarMatch>($"bad state: current user is null?");
            }

            Stopwatch stepTimer = Stopwatch.StartNew();

            BarMatchProcessing processing = new();
            processing.ReplayDownloaded = DateTime.UtcNow;

            string contentType = Request.ContentType ?? "";
            if (string.IsNullOrWhiteSpace(contentType) || !contentType.Contains("multipart/", StringComparison.OrdinalIgnoreCase)) {
                return ApiBadRequest<BarMatch>($"ContentType '{contentType}' is not a multipart-upload");
            }

            MediaTypeHeaderValue type = MediaTypeHeaderValue.Parse(Request.ContentType);
            string boundary = HeaderUtilities.RemoveQuotes(type.Boundary).Value ?? "";

            if (string.IsNullOrWhiteSpace(boundary)) {
                return ApiBadRequest<BarMatch>($"boundary from ContentType '{Request.ContentType}' was is null or empty ({type}) ({type.Boundary})");
            }

            MultipartReader reader = new(boundary, Request.Body);
            MultipartSection? part = await reader.ReadNextSectionAsync(cancel);

            if (part == null) {
                return ApiBadRequest<BarMatch>($"Multipart section missing");
            }

            bool hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(part.ContentDisposition, out ContentDispositionHeaderValue? contentDisposition);
            if (hasContentDispositionHeader == false) {
                return ApiInternalError<BarMatch>($"failed to get {nameof(contentDisposition)} from {part.ContentDisposition}");
            }

            if (contentDisposition == null) {
                return ApiInternalError<BarMatch>($"failed to get {nameof(contentDisposition)} from {part.ContentDisposition}");
            }

            if (!HasFileContentDisposition(contentDisposition)) {
                _Logger.LogError($"not a file content disposition [contentDisposition={contentDisposition}]");
                return ApiBadRequest<BarMatch>($"not a file content disposition");
            }

            string originalName = contentDisposition.FileName.Value ?? "";
            _Logger.LogDebug($"demofile uploaded [originalName={originalName}]");

            string? extension = HeaderUtilities.RemoveQuotes(Path.GetExtension(originalName)).Value;
            if (string.IsNullOrWhiteSpace(extension)) {
                return ApiBadRequest<BarMatch>($"extension from name {originalName} is null or empty");
            }

            if (extension != ".sdfz") {
                return ApiBadRequest<BarMatch>($"expected .sdfz as file extension, got '{extension}' instead");
            }

            using MemoryStream ms = new();
            await part.Body.CopyToAsync(ms, cancel);

            byte[] data = ms.ToArray();
            long uploadMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();
            processing.ReplayDownloadedMs = (int)uploadMs;

            Result<BarMatch, string> parsed = await _DemofileParser.Parse(originalName, data, cancel);
            if (parsed.IsOk == false) {
                return ApiBadRequest<BarMatch>($"failed to parse replay file: {parsed.Error}");
            }
            long parseMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();
            processing.ReplayParsedMs = (int)parseMs;
            processing.ReplayParsed = DateTime.UtcNow;

            BarMatch match = parsed.Value;
            match.UploadedByID = currentUser.ID;

            BarMatch? existing = await _MatchRepository.GetByID(match.ID, cancel);
            if (existing != null) {
                _Logger.LogDebug($"demofile already exists in the database [gameID={match.ID}]");
                return ApiOk(existing);
            }

            BarMatchProcessing? proc = await _ProcessingRepository.GetByGameID(match.ID, cancel);
            if (proc != null) {
                _Logger.LogDebug($"this game has been noticed and is processing in some way, but hasn't been parsed into a match yet");
                return ApiBadRequest<BarMatch>($"this game has already been seen, but is being processed, please wait!");
            }

            if (match.AiPlayers.Count > 0) {
                return ApiBadRequest<BarMatch>($"Games with AI players are not allowed to be uploaded");
            }

            _Logger.LogInformation($"new demofile has been uploaded [gameID={match.ID}]");

            //
            // do not accept the name of the file the user provides. this could be used to overwrite data
            //

            // 2025-02-02_20-35-40-247_Special Reef 1_2025.01.6.sdfz
            // {DATE}_{TIME_UTC}_{Map}_{Engine}.sdfz
            // it's impossible to recreate the correct demofile name from the info in the demofile
            //		(demofile does not have the millisecond start time, which the demofile uses)

            // https://github.com/beyond-all-reason/RecoilEngine/blob/0aa5469497c42b9066a304f13f76ea460aa69b07/rts/System/LoadSave/DemoRecorder.cpp#L156
            // map name gets truncated by one '.' for some reason
            //		ex: Map name_1.1 => Map_name_1
            string parsedName = $"{match.StartTime:yyyy-MM-dd}_{match.StartTime:HH-mm-ss-fff}_{match.Map}_{match.Engine}.sdfz";
            if (originalName != parsedName) {
                //_Logger.LogWarning($"demofile name did not match expected value [original='{originalName}'] [parsed='{parsedName}']");
            }

            match.FileName = parsedName;

            string replayLocation = Path.Join(_Options.Value.ReplayLocation, parsedName);
            if (System.IO.File.Exists(replayLocation) == true) {
                _Logger.LogError($"the replay file exists, but the DB data is missing! [gameID={match.ID}] [replayLocation='{replayLocation}']");
                return ApiInternalError<BarMatch>($"failsafe: the database has inconsistent data, this is an unexpected state");
            }

            match.MapName = (await _MapRepository.GetByName(match.Map, CancellationToken.None))?.FileName ?? "";

            // at this point, we do NOT want to respect a user cancellation, as this could lead to inconsistent/half-written files
            using FileStream fileStream = System.IO.File.OpenWrite(replayLocation);
            ms.Position = 0; // move back to start for copy to file
            await ms.CopyToAsync(fileStream, CancellationToken.None);

            if (System.IO.File.Exists(replayLocation) == false) {
                _Logger.LogError($"failsafe: the replay file must exist at the expected location at this point [replayLocation='{replayLocation}']");
                return ApiInternalError<BarMatch>($"failsafe: missing replay file after copying it to the disk?");
            }

            processing.GameID = match.ID;
            processing.Priority = await _PriorityCalculator.Calculate(match, CancellationToken.None);
            await _ProcessingRepository.Upsert(processing);

            // while this isn't strictly necessary due to the info already being here, this is needed if re-parsing locally
            BarReplay replay = new();
            replay.ID = match.ID;
            replay.MapName = match.MapName;
            replay.FileName = parsedName;
            await _ReplayDb.Insert(replay, CancellationToken.None);

            await _DemofileProcessor.Process(match, CancellationToken.None);

            if (processing.Priority == -1) {
                _RunQueue.Queue(new HeadlessRunQueueEntry() {
                    GameID = match.ID
                });
            }

            _Logger.LogInformation($"user uploaded match [gameID={match.ID}]");

            return ApiOk(match);
        }

        internal static bool HasFileContentDisposition(ContentDispositionHeaderValue contentDisposition) {
            // Content-Disposition: form-data; name="myfile1"; filename="Misc 002.jpg"
            return contentDisposition != null
                && contentDisposition.DispositionType.Equals("form-data")
                && (!string.IsNullOrEmpty(contentDisposition.FileName.Value)
                    || !string.IsNullOrEmpty(contentDisposition.FileNameStar.Value));
        }

    }
}
