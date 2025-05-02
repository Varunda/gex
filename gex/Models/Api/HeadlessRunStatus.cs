using System;

namespace gex.Models.Api {

	public class HeadlessRunStatus {

		/// <summary>
		///		ID of the game
		/// </summary>
		public string GameID { get; set; } = "";

		/// <summary>
		///		is the game still starting up (false), or is the game actually running? (true)
		/// </summary>
		public bool Simulating { get; set; }

		/// <summary>
		///		what frame the local simulation is on
		/// </summary>
		public long Frame { get; set; }

		/// <summary>
		///		how many frames will be simulated locally
		/// </summary>
		public long DurationFrames { get; set; }

		/// <summary>
		///		when this data was last updated
		/// </summary>
		public DateTime Timestamp { get; set; }

		/// <summary>
		///		how many frames per second are being processed
		/// </summary>
		public double Fps { get; set; }

	}
}
