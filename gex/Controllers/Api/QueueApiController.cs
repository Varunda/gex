using gex.Code;
using gex.Models;
using gex.Models.Internal;
using gex.Models.Queues;
using gex.Services.Queues;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace gex.Controllers.Api {

    [Route("/api/queue")]
    [ApiController]
    public class QueueApiController : ApiControllerBase {

        private readonly ILogger<QueueApiController> _Logger;
        private readonly BaseQueue<HeadlessRunQueueEntry> _HeadlessRunQueue;
        private readonly IServiceProvider _Services;

        private readonly IMemoryCache _Cache;

        private const string CACHE_KEY_HEADLESS_QUEUE = "Gex.Api.Queue.HeadlessEntries";

        public QueueApiController(ILogger<QueueApiController> logger,
            BaseQueue<HeadlessRunQueueEntry> headlessRunQueue, IMemoryCache cache,
            IServiceProvider services) {

            _Logger = logger;
            _HeadlessRunQueue = headlessRunQueue;
            _Cache = cache;
            _Services = services;
        }

        /// <summary>
        ///		get a list of matches that are in the queue to be replayed headlessly
        /// </summary>
        /// <response code="200">
        ///		a list of all <see cref="HeadlessRunQueueEntry"/>s, which represent all games that are
        ///		queued to be locally replayed
        /// </response>
        [HttpGet("headless")]
        public ApiResponse<List<HeadlessRunQueueEntry>> GetHeadlessQueue() {
            if (_Cache.TryGetValue(CACHE_KEY_HEADLESS_QUEUE, out List<HeadlessRunQueueEntry>? entries) == false || entries == null) {
                entries = _HeadlessRunQueue.ToList();

                _Cache.Set(CACHE_KEY_HEADLESS_QUEUE, entries, new MemoryCacheEntryOptions() {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
                });
            }

            return ApiOk(entries);
        }

        /// <summary>
        ///     debug action to clear all entries in a queue
        /// </summary>
        /// <param name="queueName">name of the queue. this is the name of the type, such as DiscordMessageQueue</param>
        /// <returns></returns>
        [PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
        [HttpPost("clear/{queueName}")]
        public ApiResponse ClearQueue(string queueName) {

            // queue clearing is broken, fix later
            return ApiOk();

            /*
            // "gex.Services.Queues.BaseQueue`1[[gex.Models.Discord.AppDiscordMessage]]"
            string typeName = $"gex.Services.Queues.BaseQueue`1[[{queueName}]]";

            Type? queueType = Type.GetType(typeName);
            _Logger.LogInformation($"looking for queue to disable [queueName={queueName}] [typeName={typeName}] [queueType={queueType?.FullName}]");
            if (queueType == null) {
                return ApiNotFound($"typeName: {typeName}");
            }

            object? queue = _Services.GetService(queueType);
            if (queue == null) {
                return ApiNotFound($"service: {queueType.FullName}");
            }

            if (queue is IProcessQueue bq) {
                bq.Clear();
                _Logger.LogInformation($"cleared queue [queue={bq.GetType().FullName}]");
            } else {
                return ApiInternalError($"failed to cast queue to a better type");
            }

            return ApiOk();
            */
        }

    }
}
