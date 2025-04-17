using System;
using System.Security.Permissions;

namespace gex.Models.Internal {

	public class HeadlessStatus {

		public int ID { get; private set; }

		public HeadlessProcessStatus? Status { get; set; }

		public HeadlessStatus(int id) {
			ID = id;
		}

	}

	public class HeadlessProcessStatus {

		public string GameID { get; set; } = "";

		public DateTime StartedAt { get; set; }

		public long ProcessID { get; set; }

	}

}
