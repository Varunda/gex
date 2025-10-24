using gex.Code;
using gex.Common.Models;
using gex.Models;
using gex.Models.Db;
using gex.Models.Internal;
using gex.Models.Queues;
using gex.Services;
using gex.Services.Db.Match;
using gex.Services.Queues;
using gex.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Controllers.Api {

    [ApiController]
    [Route("/api/match-processing")]
    public class BarMatchProcessingApiController : ApiControllerBase {

        private readonly ILogger<BarMatchProcessingApiController> _Logger;
        private readonly BarMatchProcessingRepository _ProcessingRepository;
        private readonly ICurrentAccount _CurrentUser;
        private readonly BaseQueue<HeadlessRunQueueEntry> _HeadlessRunQueue;
        private readonly ServiceHealthMonitor _ServiceHealthMonitor;

        public BarMatchProcessingApiController(ILogger<BarMatchProcessingApiController> logger,
            BarMatchProcessingRepository processingRepository, ICurrentAccount currentUser,
            BaseQueue<HeadlessRunQueueEntry> headlessQueue, ServiceHealthMonitor serviceHealthMonitor) {

            _Logger = logger;
            _ProcessingRepository = processingRepository;
            _CurrentUser = currentUser;
            _HeadlessRunQueue = headlessQueue;
            _ServiceHealthMonitor = serviceHealthMonitor;
        }

        /// <summary>
        ///		get the <see cref="BarMatchProcessing"/> of a match
        /// </summary>
        /// <param name="gameID">ID of the <see cref="BarMatchProcessing"/> to get</param>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///		the response will contain the <see cref="BarMatchProcessing"/> with <see cref="BarMatchProcessing.GameID"/>
        ///		of <paramref name="gameID"/>
        /// </response>
        /// <response code="204">
        ///		no <see cref="BarMatchProcessing"/> with <see cref="BarMatchProcessing.GameID"/> of <paramref name="gameID"/> exists
        /// </response>
        [HttpGet("{gameID}")]
        public async Task<ApiResponse<BarMatchProcessing>> GetByGameID(string gameID, CancellationToken cancel) {
            BarMatchProcessing? proc = await _ProcessingRepository.GetByGameID(gameID, cancel);
            if (proc == null) {
                return ApiNoContent<BarMatchProcessing>();
            }

            return ApiOk(proc);
        }

        /// <summary>
        ///		get a list of the top 100 games to be processed via the priority system
        /// </summary>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///		a list of <see cref="BarMatchProcessing"/> that represents an ordered list of games
        ///		as they will be prioritized. note that this list can change, and is not set in stone
        /// </response>
        [HttpGet("priority")]
        public async Task<ApiResponse<List<BarMatchProcessing>>> GetPriorityList(CancellationToken cancel) {
            List<BarMatchProcessing> list = await _ProcessingRepository.GetPriorityList(cancel);
            return ApiOk(list);
        }

        /// <summary>
        ///     request Gex priorizes a <see cref="BarMatch"/>. This requires a Discord account, 
        ///     and reduces the priority by 20 for each user. a user can only prioritize one game
        /// </summary>
        /// <param name="gameID">ID of the <see cref="BarMatch"/> to lower the priority of</param>
        /// <param name="cancel">cancellation request</param>
        /// <response code="200">
        ///     the <see cref="BarMatch"/> with <see cref="BarMatch.ID"/> of <paramref name="gameID"/>
        ///     successfully had the <see cref="BarMatchProcessing.Priority"/> lowered by 20
        /// </response>
        /// <response code="400">
        ///     the <see cref="BarMatchProcessing"/> with <see cref="BarMatchProcessing.GameID"/>
        ///     of <paramref name="gameID"/> has already been simulated
        /// </response>
        /// <response code="404">
        ///     no <see cref="BarMatch"/> with <see cref="BarMatch.ID"/> of <paramref name="gameID"/> exists
        /// </response>
        [HttpPost("prioritize/{gameID}")]
        public async Task<ApiResponse> PrioritizeGame(string gameID, CancellationToken cancel) {
            AppAccount? currentUser = await _CurrentUser.Get(cancel);
            if (currentUser == null) {
                return ApiAuthorize();
            }

            BarMatchProcessing? proc = await _ProcessingRepository.GetByGameID(gameID, cancel);
            if (proc == null) {
                return ApiNotFound($"{nameof(BarMatch)} {gameID}");
            }

            if (proc.ReplaySimulated != null) {
                return ApiBadRequest($"game {gameID} has already been processed");
            }

            await _ProcessingRepository.PrioritizeGame(currentUser.DiscordID, gameID, cancel);

            return ApiOk();
        }

        /// <summary>
        ///     developer action to force a game to be replayed on Gex.
        ///     use carefully, this does not check if a game is in the process of being replayed.
        ///     NOTE: if the <see cref="HeadlessRunQueue"/> service is disabled, then this will do nothing
        /// </summary>
        /// <param name="gameID">ID of the game to force a replay of</param>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///     the <see cref="BarMatch"/> with <see cref="BarMatch.ID"/> of <paramref name="gameID"/> was successfully
        ///     put into the <see cref="HeadlessRunQueue"/> to be ran
        /// </response>
        [HttpPost("run/{gameID}")]
        [PermissionNeeded(AppPermission.GEX_MATCH_FORCE_REPLAY)]
        public async Task<ApiResponse> ForceGameRun(string gameID, CancellationToken cancel) {
            BarMatchProcessing? proc = await _ProcessingRepository.GetByGameID(gameID, cancel);
            if (proc == null) {
                return ApiNotFound($"{nameof(BarMatch)} {gameID}");
            }

            _Logger.LogInformation($"forcing game to be ran headlessly [gameID={gameID}]");

            ServiceHealthEntry? runQueueService = _ServiceHealthMonitor.Get("headless_run_queue_processor");
            if (runQueueService?.Enabled ?? false == false) {
                _Logger.LogWarning($"while game was forced to be ran, "
                    + $"please note the headless_run_queue_processor service is disabled, so this game will NOT be ran [gameID={gameID}]");
            }

            _HeadlessRunQueue.Queue(new HeadlessRunQueueEntry() {
                GameID = gameID,
                Force = true,
                ForceForward = true
            });

            return ApiOk();
        }

        /// <summary>
        ///     action to change the priority of a <see cref="BarMatchProcessing"/>
        /// </summary>
        /// <param name="gameID">ID of the <see cref="BarMatchProcessing"/> to change the <see cref="BarMatchProcessing.Priority"/> of</param>
        /// <param name="priority">new priority to set. cannot be below -1</param>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///     the <see cref="BarMatchProcessing"/> with <see cref="BarMatchProcessing.GameID"/>
        ///     of <paramref name="gameID"/> successfully had its <see cref="BarMatchProcessing.Priority"/>
        ///     set to <paramref name="priority"/>
        /// </response>
        /// <response code="404">
        ///     no <see cref="BarMatchProcessing"/> with <see cref="BarMatchProcessing.GameID"/>
        ///     of <paramref name="gameID"/> exists
        /// </response>
        [HttpPost("update-priority/{gameID}/{priority}")]
        [PermissionNeeded(AppPermission.GEX_MATCH_FORCE_REPLAY)]
        public async Task<ApiResponse> SetMatchProcessingPriority(string gameID, short priority, CancellationToken cancel) {
            if (priority < -1) {
                return ApiBadRequest($"priority cannot be below -1");
            }

            BarMatchProcessing? proc = await _ProcessingRepository.GetByGameID(gameID, cancel);
            if (proc == null) {
                return ApiNotFound($"{nameof(BarMatch)} {gameID}");
            }

            _Logger.LogInformation($"updating priority of match [gameID={gameID}] [priority={priority}]");

            proc.Priority = priority;
            await _ProcessingRepository.Upsert(proc);

            return ApiOk();
        }

    }
}
