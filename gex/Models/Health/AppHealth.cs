using gex.Models.Api;
using System;
using System.Collections.Generic;

namespace gex.Models.Health {

    /// <summary>
    ///     Information about the health of the app
    /// </summary>
    public class AppHealth {

        /// <summary>
        ///     Information about the hosted queues in Gex
        /// </summary>
        public List<ServiceQueueCount> Queues { get; set; } = [];

        public List<ServiceHealthEntry> Services { get; set; } = [];

        public List<HeadlessRunStatus> HeadlessRuns { get; set; } = [];

        /// <summary>
        ///     When this data was created
        /// </summary>
        public DateTime Timestamp { get; set; }

    }
}
