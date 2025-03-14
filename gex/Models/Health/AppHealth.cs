﻿using System;
using System.Collections.Generic;
using gex.Models.Api;

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

        /// <summary>
        ///     When this data was created
        /// </summary>
        public DateTime Timestamp { get; set; }

    }
}
