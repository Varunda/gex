using gex.Models;
using gex.Models.Queues;
using gex.Services.Queues;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gex.Controllers.Api {

	[Route("/api/queue")]
	[ApiController]
	public class QueueApiController : ApiControllerBase {

		private readonly ILogger<QueueApiController> _Logger;
		private readonly BaseQueue<HeadlessRunQueueEntry> _HeadlessRunQueue;

		private readonly IMemoryCache _Cache;

		private const string CACHE_KEY_HEADLESS_QUEUE = "Gex.Api.Queue.HeadlessEntries";

		public QueueApiController(ILogger<QueueApiController> logger,
			BaseQueue<HeadlessRunQueueEntry> headlessRunQueue, IMemoryCache cache) {

			_Logger = logger;
			_HeadlessRunQueue = headlessRunQueue;
			_Cache = cache;
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

	}
}
