using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using gex.Models;
using gex.Models.Api;
using gex.Models.Health;
using gex.Services.Queues;
using gex.Code;
using gex.Services;
using gex.Models.Queues;
using gex.Services.Repositories;
using Microsoft.AspNetCore.RateLimiting;
using gex.Models.Internal;
using Microsoft.AspNetCore.Authorization;

namespace gex.Controllers.Api {

    [ApiController]
    [Route("api/health")]
    public class HealthApiController : ApiControllerBase {

        private readonly ILogger<HealthApiController> _Logger;
        private readonly IMemoryCache _Cache;

        private readonly ServiceHealthMonitor _ServiceHealthMonitor;
		private readonly HeadlessRunStatusRepository _HeadlessRunStatusRepository;

        private readonly DiscordMessageQueue _DiscordQueue;
        private readonly BaseQueue<GameReplayDownloadQueueEntry> _DownloadQueue;
        private readonly BaseQueue<GameReplayParseQueueEntry> _ParseQueue;
        private readonly BaseQueue<HeadlessRunQueueEntry> _HeadlessRunQueue;
        private readonly BaseQueue<ActionLogParseQueueEntry> _ActionLogQueue;
        private readonly BaseQueue<UserMapStatUpdateQueueEntry> _UserMapStatUpdateQueue;
        private readonly BaseQueue<UserFactionStatUpdateQueueEntry> _FactionStatUpdateQueue;
		private readonly BaseQueue<HeadlessRunStatus> _HeadlessRunStatusQueue;
		private readonly BaseQueue<MapStatUpdateQueueEntry> _MapStatUpdateQueue;

		public HealthApiController(ILogger<HealthApiController> logger, IMemoryCache cache,
			DiscordMessageQueue discordQueue, BaseQueue<HeadlessRunQueueEntry> headlessRunQueue,
			ServiceHealthMonitor serviceHealthMonitor, BaseQueue<GameReplayDownloadQueueEntry> downloadQueue,
			BaseQueue<GameReplayParseQueueEntry> parseQueue, BaseQueue<ActionLogParseQueueEntry> actionLogQueue,
			BaseQueue<UserMapStatUpdateQueueEntry> userMapStatUpdateQueue, BaseQueue<UserFactionStatUpdateQueueEntry> factionStatUpdateQueue,
			BaseQueue<HeadlessRunStatus> headlessRunStatusQueue, HeadlessRunStatusRepository headlessRunStatusRepository,
			BaseQueue<MapStatUpdateQueueEntry> mapStatUpdateQueue) {

			_Logger = logger;
			_Cache = cache;

			_DiscordQueue = discordQueue;
			_HeadlessRunQueue = headlessRunQueue;
			_ServiceHealthMonitor = serviceHealthMonitor;
			_DownloadQueue = downloadQueue;
			_ParseQueue = parseQueue;
			_ActionLogQueue = actionLogQueue;
			_UserMapStatUpdateQueue = userMapStatUpdateQueue;
			_MapStatUpdateQueue = mapStatUpdateQueue;
			_FactionStatUpdateQueue = factionStatUpdateQueue;
			_HeadlessRunStatusQueue = headlessRunStatusQueue;
			_HeadlessRunStatusRepository = headlessRunStatusRepository;
		}

		/// <summary>
		///		disable a service
		/// </summary>
		/// <param name="name">name of the service</param>
		/// <returns></returns>
		[PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
		[HttpPost("disable/{name}")]
		[Authorize]
		public ApiResponse DisableService(string name) {

			ServiceHealthEntry? entry = _ServiceHealthMonitor.Get(name);
			if (entry == null) {
				return ApiNotFound($"{nameof(ServiceHealthEntry)} {name}");
			}

			entry.Enabled = false;

			return ApiOk();
		}

		/// <summary>
		///		enable a service
		/// </summary>
		/// <param name="name">name of the service</param>
		/// <returns></returns>
		[PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
		[HttpPost("enable/{name}")]
		[Authorize]
		public ApiResponse EnableService(string name) {

			ServiceHealthEntry? entry = _ServiceHealthMonitor.Get(name);
			if (entry == null) {
				return ApiNotFound($"{nameof(ServiceHealthEntry)} {name}");
			}

			entry.Enabled = true;

			return ApiOk();
		}

		/// <summary>
		///     Get an object that indicates how healthy Gex is in various metrics
		/// </summary>
		/// <remarks>
		///     Feel free to hammer this endpoint as much as you'd like. The results are cached for 800ms, and it only takes like 2ms to
		///     get all the data, so hitting this endpoint is not a burden
		/// </remarks>
		/// <response code="200">
		///     The response will contain a <see cref="AppHealth"/> that represents the health of the app at the time of being called
		/// </response>
		[HttpGet]
		[DisableRateLimiting]
        public ApiResponse<AppHealth> GetRealtimeHealth() {
            if (_Cache.TryGetValue("App.Health", out AppHealth? health) == false || health == null) {
                health = new AppHealth();
                health.Timestamp = DateTime.UtcNow;

                health.Queues = new List<ServiceQueueCount>() {
                    _MakeCount("discord_message_queue", _DiscordQueue),
                    _MakeCount("replay_download_queue", _DownloadQueue),
                    _MakeCount("replay_parse_queue", _ParseQueue),
                    _MakeCount("headless_run_queue", _HeadlessRunQueue),
                    _MakeCount("action_log_queue", _ActionLogQueue),
                    _MakeCount("user_map_stat_update_queue", _MapStatUpdateQueue),
                    _MakeCount("user_faction_stat_update_queue", _FactionStatUpdateQueue),
					_MakeCount("headless_run_update_queue", _HeadlessRunStatusQueue),
					_MakeCount("map_stat_update_queue", _MapStatUpdateQueue)
                };

                foreach (string service in _ServiceHealthMonitor.GetServices()) {
                    ServiceHealthEntry? entry = _ServiceHealthMonitor.Get(service);
                    if (entry != null) {
                        health.Services.Add(entry);
                    }
                }

				health.HeadlessRuns = _HeadlessRunStatusRepository.GetAll();

                _Cache.Set("App.Health", health, new MemoryCacheEntryOptions() {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(800)
                });
            }

            return ApiOk(health);
        }

        private ServiceQueueCount _MakeCount(string name, IProcessQueue queue) {
            ServiceQueueCount c = new() {
                QueueName = name,
                Count = queue.Count() ,
                Processed = queue.Processed()
            };

            List<long> times = queue.GetProcessTime();
            if (times.Count > 0) {
                c.Average = times.Average();
                c.Min = times.Min();
                c.Max = times.Max();

                List<long> sorted = times.OrderBy(i => i).ToList();
                int mid = sorted.Count / 2;
                if (sorted.Count % 2 == 0) {
                    c.Median = (sorted.ElementAt(mid - 1) + sorted.ElementAt(mid)) / 2;
                } else {
                    c.Median = sorted.ElementAt(mid);
                }
            }

            return c;
        }

    }
}
