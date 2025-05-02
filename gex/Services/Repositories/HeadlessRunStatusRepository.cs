using gex.Models.Api;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace gex.Services.Repositories {

	public class HeadlessRunStatusRepository {

		private readonly ILogger<HeadlessRunStatusRepository> _Logger;

		private static ConcurrentDictionary<string, HeadlessRunStatus> _Data = [];

		public HeadlessRunStatusRepository(ILogger<HeadlessRunStatusRepository> logger) {
			_Logger = logger;
		}

		public HeadlessRunStatus? Get(string gameID) {
			lock (_Data) {
				_Data.TryGetValue(gameID, out HeadlessRunStatus? status);
				return status;
			}
		}

		public List<HeadlessRunStatus> GetAll() {
			lock (_Data) {
				return _Data.Values.ToList();
			}
		}

		public void Upsert(string gameID, HeadlessRunStatus status) {
			lock (_Data) {
				_Data.AddOrUpdate(gameID, status, (key, value) => {
					return status;
				});
			}
		}

		public void Remove(string gameID) {
			lock (_Data) {
				_Data.Remove(gameID, out _);
			}
		}

	}
}
