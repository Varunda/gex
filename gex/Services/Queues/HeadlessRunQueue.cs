﻿using gex.Models.Queues;
using gex.Services.Metrics;
using Microsoft.Extensions.Logging;

namespace gex.Services.Queues {

    public class HeadlessRunQueue : BaseQueue<HeadlessRunQueueEntry> {

        public HeadlessRunQueue(ILoggerFactory factory, QueueMetric metrics) : base(factory, metrics) { }

    }
}
