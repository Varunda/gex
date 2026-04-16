using gex.Common.Models;
using gex.Common.Models.Options;
using gex.Models.Event;
using gex.Services.BarApi;
using gex.Services.Storage;
using gex.Tests.Util;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Tests.Services.BarApi {

    [TestClass]
    public class ActionLogParserTest {

        [TestMethod]
        public async Task test_parse() {
            GameOutputStorage storage = new(new TestLogger<GameOutputStorage>(),
                options: Options.Create<FileStorageOptions>(new FileStorageOptions() {
                    GameLogLocation = "./resources/action_logs"
                })
            );

            ActionLogParser parser = new ActionLogParser(new TestLogger<ActionLogParser>(), storage);

            Result<GameOutput, string> output = await parser.Parse("aa_actions_with_nan_and_inf", CancellationToken.None);
            Assert.IsTrue(output.IsOk, $"output failed: {output.Error}");
        }

    }
}
