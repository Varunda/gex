using gex.Common.Models;
using gex.Common.Models.Event;
using gex.Common.Models.Options;
using gex.Common.Services.Parser;
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

            string gameID = "aa_actions_with_nan_and_inf";

            Result<string, string> actionLog = await storage.GetActionLog(gameID, CancellationToken.None);

            ActionLogParser parser = new ActionLogParser(new TestLogger<ActionLogParser>());

            Result<GameOutput, string> output = parser.Parse("aa_actions_with_nan_and_inf", actionLog.Value, CancellationToken.None);
            Assert.IsTrue(output.IsOk, $"output failed: {output.Error}");
        }

    }
}
