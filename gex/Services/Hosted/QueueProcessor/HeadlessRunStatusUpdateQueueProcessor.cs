using gex.Code.Hubs;
using gex.Models.Api;
using gex.Services.Queues;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.QueueProcessor {

	public class HeadlessRunStatusUpdateQueueProcessor : BaseQueueProcessor<HeadlessRunStatus> {

		private readonly IHubContext<HeadlessReplayHub, IHeadlessReplayHub> _HeadlessReplayHub;

		public HeadlessRunStatusUpdateQueueProcessor(ILoggerFactory factory, BaseQueue<HeadlessRunStatus> queue,
			ServiceHealthMonitor serviceHealthMonitor,
			IHubContext<HeadlessReplayHub, IHeadlessReplayHub> headlessReplayHub)
		: base("headless_run_status_update_queue", factory, queue, serviceHealthMonitor) {

			_HeadlessReplayHub = headlessReplayHub;
		}

		protected override async Task<bool> _ProcessQueueEntry(HeadlessRunStatus entry, CancellationToken cancel) {
			string group = $"Gex.Headless.{entry.GameID}";

			await _HeadlessReplayHub.Clients.Group(group).UpdateProgress(entry);

			return true;
		}

	}
}
