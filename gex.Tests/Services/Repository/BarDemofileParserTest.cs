using gex.Models;
using gex.Models.Db;
using gex.Models.Demofile;
using gex.Services.Parser;
using gex.Tests.Util;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Tests.Services.Repository {

    [TestClass]
    public class BarDemofileParserTest {

        [DataTestMethod]
        [DataRow("test.sdfz")]
        [DataRow("test2.sdfz")]
        [DataRow("test3.sdfz")]
        public async Task testAsync(string file) {
            TestLogger<BarDemofileParser> logger = new TestLogger<BarDemofileParser>();

            logger.LogInformation($"cwd: {Environment.CurrentDirectory}");

            using FileStream testInput = File.OpenRead($"./resources/{file}");
            using MemoryStream ms = new();
            await testInput.CopyToAsync(ms);

            byte[] input = ms.ToArray();

            BarDemofileParser parser = new(logger);

            Result<BarMatch, string> output = await parser.Parse("", input, CancellationToken.None);
            if (output.IsOk == false) {
                logger.LogError(output.Error);
            }

            Assert.IsTrue(output.IsOk);
        }

        [TestMethod]
        public async Task Test_AntiZipBomb() {
            TestLogger<BarDemofileParser> logger = new TestLogger<BarDemofileParser>();

            logger.LogInformation($"cwd: {Environment.CurrentDirectory}");

            byte[] comp = await File.ReadAllBytesAsync($"./resources/bomb.gzip");
            BarDemofileParser parser = new(logger);

            Result<BarMatch, string> output = await parser.Parse("", comp, CancellationToken.None);
            Assert.IsTrue(!output.IsOk);
        }

    }
}
