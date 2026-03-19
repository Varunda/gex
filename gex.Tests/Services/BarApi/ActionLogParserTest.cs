using gex.Common.Models;
using gex.Models.Event;
using gex.Services.BarApi;
using gex.Tests.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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

        [DataTestMethod]
        [DataRow("actions_with_nan_and_inf.json")]
        public async Task test_parse(string filename) {
            ActionLogParser parser = new ActionLogParser(new TestLogger<ActionLogParser>());

            Result<GameOutput, string> output = await parser.Parse("", $"./resources/{filename}", CancellationToken.None);
            Assert.IsTrue(output.IsOk, $"output failed: {output.Error}");


        }

    }
}
