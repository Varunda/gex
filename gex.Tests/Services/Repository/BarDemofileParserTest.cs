using gex.Common.Models;
using gex.Common.Models.Options;
using gex.Common.Services;
using gex.Common.Services.Bar;
using gex.Models.Db;
using gex.Services.BarApi;
using gex.Services.Db;
using gex.Services.Metrics;
using gex.Services.Parser;
using gex.Services.Repositories;
using gex.Services.Util;
using gex.Tests.Util;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.BouncyCastle.Asn1.X509.Qualified;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Tests.Services.Repository {

    [TestClass]
    public class BarDemofileParserTest {

        private async Task<(BarDemofileParser, ServiceProvider)> _Get() {
            ServiceCollection services = await Service.Standard();
            services.AddSingleton<LuaCommandParser>();
            services.AddSingleton<BarDemofileParser>();

            ServiceProvider svs = services.BuildServiceProvider();

            return (svs.GetRequiredService<BarDemofileParser>(), svs);
        }

        [DataTestMethod]
        [DataRow("test.sdfz")]
        [DataRow("test2.sdfz")]
        [DataRow("test3.sdfz")]
        [DataRow("BAR105_2590_map_draw.sdfz")]
        [DataRow("2025.04.08_map_draw.sdfz")]
        [DataRow("2025.06.06_map_draw_test.sdfz")]
        [DataRow("BAR105_1821.sdfz")]
        [DataRow("2025.06.19_IndexOutOfRange.sdfz")]
        public async Task testAsync(string file) {
            TestLogger<BarDemofileParser> logger = new TestLogger<BarDemofileParser>();

            logger.LogInformation($"cwd: {Environment.CurrentDirectory}");

            using FileStream testInput = File.OpenRead($"./resources/{file}");
            using MemoryStream ms = new();
            await testInput.CopyToAsync(ms);

            byte[] input = ms.ToArray();

            (BarDemofileParser parser, _) = await _Get();

            Result<BarMatch, string> output = await parser.Parse("", input, new DemofileParserOptions() {
                IncludeMapDraws = true,
                IncludeCommands = true,
            }, CancellationToken.None);
            if (output.IsOk == false) {
                logger.LogError(output.Error);
            }

            Assert.IsTrue(output.IsOk);
            logger.LogInformation($"engine: {output.Value.Engine}, game: {output.Value.GameVersion}");
        }

        /// <summary>
        ///     ensure that the anti-zip bomb tests prevent the a zip bomb from exploding
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task Test_AntiZipBomb() {
            TestLogger<BarDemofileParser> logger = new TestLogger<BarDemofileParser>();

            logger.LogInformation($"cwd: {Environment.CurrentDirectory}");

            byte[] comp = await File.ReadAllBytesAsync($"./resources/bomb.gzip");
            (BarDemofileParser parser, _) = await _Get();

            Result<BarMatch, string> output = await parser.Parse("", comp, new DemofileParserOptions() {
                IncludeCommands = true,
                IncludeMapDraws = true,
            }, CancellationToken.None);
            Assert.IsTrue(!output.IsOk);
        }

        /// <summary>
        ///     tests a game specifically from 2025.04.08 that uses packet ID 31 for MAPDRAW, but has coords of u32
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task Test_PacketID31_With_u32_Coords_Using_202504() {
            TestLogger<BarDemofileParser> logger = new TestLogger<BarDemofileParser>();

            using FileStream testInput = File.OpenRead("./resources/2025.04.08_map_draw.sdfz");
            using MemoryStream ms = new();
            await testInput.CopyToAsync(ms);

            byte[] input = ms.ToArray();

            (BarDemofileParser parser, _) = await _Get();

            Result<BarMatch, string> output = await parser.Parse("", input, new DemofileParserOptions() {
                IncludeMapDraws = true,
                IncludeCommands = true,
            }, CancellationToken.None);
            if (output.IsOk == false) {
                logger.LogError(output.Error);
            }

            Assert.IsTrue(output.IsOk);

            //_DumpMapDrawsDebug(output.Value.MapDraws, logger);

            List<BarMatchMapDraw> draws = output.Value.MapDraws;

            Assert.AreEqual("erase", draws[0].Action);
            Assert.AreEqual(0, draws[0].X);
            Assert.AreEqual(0, draws[0].Z);
            Assert.AreEqual(13, draws[0].PlayerID);

            Assert.AreEqual("line", draws[2].Action);
            Assert.AreEqual(3570, draws[2].X);
            Assert.AreEqual(9815, draws[2].Z);
            Assert.AreEqual(3558, ((BarMatchMapDrawLine)draws[2]).EndX);
            Assert.AreEqual(9791, ((BarMatchMapDrawLine)draws[2]).EndZ);

            Assert.AreEqual("point", draws[143].Action);
            Assert.AreEqual(2994, draws[143].X);
            Assert.AreEqual(839, draws[143].Z);
            Assert.AreEqual(9, draws[143].PlayerID);
            Assert.AreEqual("core shuriken", ((BarMatchMapDrawPoint)draws[143]).Label);

            Assert.AreEqual("point", draws[197].Action);
            Assert.AreEqual("", ((BarMatchMapDrawPoint)draws[197]).Label);
        }

        /// <summary>
        ///     tests a game specifically from 2025.06.06 that uses packet ID 32 for MAPDRAW with coords of u32
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task Test_PacketID32_With_u32_Coords_Using_202506() {
            TestLogger<BarDemofileParser> logger = new TestLogger<BarDemofileParser>();

            using FileStream testInput = File.OpenRead("./resources/2025.06.06_map_draw_test.sdfz");
            using MemoryStream ms = new();
            await testInput.CopyToAsync(ms);

            byte[] input = ms.ToArray();

            (BarDemofileParser parser, _) = await _Get();

            Result<BarMatch, string> output = await parser.Parse("", input, new DemofileParserOptions() {
                IncludeMapDraws = true,
                IncludeCommands = true,
            }, CancellationToken.None);
            if (output.IsOk == false) {
                logger.LogError(output.Error);
            }

            Assert.IsTrue(output.IsOk);

            List<BarMatchMapDraw> draws = output.Value.MapDraws;

            Assert.AreEqual("erase", draws[0].Action);
            Assert.AreEqual(0, draws[0].X);
            Assert.AreEqual(0, draws[0].Z);
            Assert.AreEqual(0, draws[0].PlayerID);

            Assert.AreEqual("line", draws[3].Action);
            Assert.AreEqual(1656, draws[3].X);
            Assert.AreEqual(2519, draws[3].Z);
            Assert.AreEqual(1657, ((BarMatchMapDrawLine)draws[3]).EndX);
            Assert.AreEqual(2521, ((BarMatchMapDrawLine)draws[3]).EndZ);

            Assert.AreEqual("point", draws[13].Action);
            Assert.AreEqual(1919, draws[13].X);
            Assert.AreEqual(2641, draws[13].Z);
            Assert.AreEqual(0, draws[13].PlayerID);
            Assert.AreEqual("hi", ((BarMatchMapDrawPoint)draws[13]).Label);

            //_DumpMapDrawsDebug(output.Value.MapDraws, logger);
        }

        [TestMethod]
        public async Task Test_StartSpot_Out_Of_StartBox() {
            TestLogger<BarDemofileParser> logger = new TestLogger<BarDemofileParser>();

            using FileStream testInput = File.OpenRead("./resources/start_spot_outside_start_box.sdfz");
            using MemoryStream ms = new();
            await testInput.CopyToAsync(ms);

            byte[] input = ms.ToArray();

            (BarDemofileParser parser, ServiceProvider svs) = await _Get();
            BarMapRepository mapRepo = svs.GetRequiredService<BarMapRepository>();

            await mapRepo.GetByFileName("all_that_glitters_v2.2.3", CancellationToken.None);

            Result<BarMatch, string> output = await parser.Parse("", input, new DemofileParserOptions() {
                IncludeMapDraws = true,
                IncludeCommands = true,
            }, CancellationToken.None);
            if (output.IsOk == false) {
                logger.LogError(output.Error);
            }

            Assert.IsTrue(output.IsOk);

            Assert.AreEqual("cetme", output.Value.Players[13].Name);
            Assert.AreEqual(1161.8618, output.Value.Players[13].StartingPosition.X, 0.01);
            Assert.AreEqual(220.4636, output.Value.Players[13].StartingPosition.Y, 0.01);
            Assert.AreEqual(1968.3596, output.Value.Players[13].StartingPosition.Z, 0.01);
        }

        [TestMethod]
        public async Task Test_Missing_Player_36() {
            TestLogger<BarDemofileParser> logger = new TestLogger<BarDemofileParser>();

            using FileStream testInput = File.OpenRead("./resources/missing_player_36.sdfz");
            using MemoryStream ms = new();
            await testInput.CopyToAsync(ms);

            byte[] input = ms.ToArray();

            (BarDemofileParser parser, ServiceProvider svs) = await _Get();

            // add user for the parser to find the user ID of a late join spec
            BarUserRepository userRepo = svs.GetRequiredService<BarUserRepository>();
            await userRepo.Upsert(11212, new gex.Models.UserStats.BarUser() {
                UserID = 11212,
                Username = "Arastais",
                LastUpdated = DateTime.UtcNow,
                CountryCode = "moon"
            }, CancellationToken.None);

            Result<BarMatch, string> output = await parser.Parse("", input, new DemofileParserOptions() { }, CancellationToken.None);
            if (output.IsOk == false) {
                logger.LogError(output.Error);
            }

            Assert.IsTrue(output.IsOk);

            BarMatch match = output.Value;
            Assert.AreEqual("ae6a156861018b845447e8c075c394e0", match.ID);
            Assert.AreEqual(35, match.Spectators.Count);

            Assert.AreEqual("fightwar", match.Spectators[17].Name);
            Assert.AreEqual(20, match.Spectators[17].PlayerID);
            Assert.AreEqual(196408, match.Spectators[17].UserID);

            Assert.AreEqual(11212, match.Spectators[21].UserID);
            Assert.AreEqual("Arastais", match.Spectators[21].Name);
            Assert.IsTrue(match.Spectators[21].UserIDCanBeWrong);

        }

        /// <summary>
        ///     debug method to see the output of map draws
        /// </summary>
        /// <param name="draws"></param>
        /// <param name="logger"></param>
        private void _DumpMapDrawsDebug(List<BarMatchMapDraw> draws, ILogger logger) {
            for (int i = 0; i < draws.Count; ++i) {
                BarMatchMapDraw draw = draws[i];

                logger.LogInformation($"[{i}] {draw.PlayerID} @({draw.X},{draw.Z}) > {draw.Action}");
                if (draw.Action == "point") {
                    BarMatchMapDrawPoint point = (BarMatchMapDrawPoint)draw;
                    logger.LogInformation($"{point.Label}");
                } else if (draw.Action == "line") {
                    BarMatchMapDrawLine line = (BarMatchMapDrawLine)draw;
                    logger.LogInformation($"=> ({line.EndX}, {line.EndZ})");
                }
            }
        }

    }
}
