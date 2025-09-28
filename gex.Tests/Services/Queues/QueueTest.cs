using gex.Services.Metrics;
using gex.Services.Queues;
using gex.Tests.Util;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Tests.Services.Queues {

    [TestClass]
    public class QueueTest {

        [TestMethod]
        public async Task Queue_Basics() {
            ILoggerFactory loggerFactory = LoggerFactory.Create(options => { });
            IMeterFactory mf = new TestMeterFactory();
            QueueMetric metric = new(mf);

            BaseQueue<string> queue = new(loggerFactory, metric);
            Assert.AreEqual(0, queue.Count(), "expected default queue size to be 0");

            DateTime now = DateTime.UtcNow;
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(1));
            try {
                await queue.Dequeue(cts.Token);
            } catch (Exception ex) {
                Assert.AreEqual(typeof(OperationCanceledException), ex.GetType());
            }

            queue.Queue("hi");
            queue.Clear();
            Assert.AreEqual(0, queue.Count(), "expected queue to have 0 items in it after a .Clear()");

            Task t = Task.Run(async () => {
                Assert.AreEqual("1", await queue.Dequeue(CancellationToken.None));
                Assert.AreEqual("2", await queue.Dequeue(CancellationToken.None));
            });

            queue.Queue("1");
            queue.Queue("2");

            await t;

            Assert.AreEqual(0, queue.Count(), "expected queue to have 0 items after dequeued 2");
            queue.Queue("3");
            Assert.AreEqual(1, queue.Count(), "expected queue to have 1 item after queue");
            queue.Queue("4");
            Assert.AreEqual(2, queue.Count(), "expected queue to have 2 items after queue");
        }

        // TODO: broken test
        //[TestMethod]
        public async Task Queue_SlamClear() {
            ILoggerFactory loggerFactory = LoggerFactory.Create(options => { });
            IMeterFactory mf = new TestMeterFactory();
            QueueMetric metric = new(mf);

            BaseQueue<string> queue = new(loggerFactory, metric);

            DateTime start = DateTime.UtcNow;

            bool errored = false;

            Task t = new(async () => {
                while ((DateTime.UtcNow - start) <= TimeSpan.FromSeconds(1)) {
                    try {
                        Assert.IsNotNull(await queue.Dequeue(CancellationToken.None));
                    } catch (Exception ex) {
                        errored = true;
                        Console.Error.WriteLine($"{ex.Message}");
                        break;
                    }
                }
            });

            Task t2 = new(() => {
                while ((DateTime.UtcNow - start) <= TimeSpan.FromSeconds(1)) {
                    queue.Clear();
                }
            });

            Task t3 = new(() => {
                while ((DateTime.UtcNow - start) <= TimeSpan.FromSeconds(1)) {
                    queue.Queue("1");
                }
            });

            t.Start();
            t2.Start();
            t3.Start();

            await t.WaitAsync(CancellationToken.None);
            await t2.WaitAsync(CancellationToken.None);
            await t3.WaitAsync(CancellationToken.None);

            if (errored == true) {
                Assert.Fail("failed during the dequeue");
            }
        }

    }
}
