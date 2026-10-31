using gex.Common.Models;
using gex.Common.Models.Match;
using gex.Common.Models.Search;
using gex.Common.Services.Db.Match;
using gex.Common.Services.Parser;
using gex.Common.Services.Repositories;
using gex.Common.Services.Util;
using gex.Services.Search;
using gex.Services.Util;
using gex.Tests.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Tests.Services.Db {

    [TestClass]
    public class BarMatchDb_SearchQueryTest {

        private static int _DemofileCount = -1;
        private static int GetDemofileCount() {
            if (_DemofileCount == -1) {
                _DemofileCount = Directory.GetFiles("./resources/", "*.sdfz").Length;
            }

            return _DemofileCount;
        }

        private static ILogger<BarMatchDb_SearchQueryTest> _Logger = new TestLogger<BarMatchDb_SearchQueryTest>();

        private static ServiceProvider? _ServiceProvier = null;
        private static async Task<(IBarMatchDb, ServiceProvider)> _Get() {
            if (_ServiceProvier == null) {
                ServiceCollection services = await Service.Standard();
                _ServiceProvier = services.BuildServiceProvider();
            }

            return (_ServiceProvier.GetRequiredService<IBarMatchDb>(), _ServiceProvier);
        }

        /// <summary>
        ///     initialize this test class by getting all demofiles in the /resources folder, 
        ///     and inserting each of them into the DB for test usage
        /// </summary>
        [ClassInitialize]
        public static async Task Init(TestContext ctx) {
            TestLogger.DisableOutput = true;

            (IBarMatchDb db, ServiceProvider svs) = await _Get();
            BarDemofileParser parser = svs.GetRequiredService<BarDemofileParser>();
            BarDemofileResultProcessor processor = svs.GetRequiredService<BarDemofileResultProcessor>();
            BarMatchProcessingRepository processingRepo = svs.GetRequiredService<BarMatchProcessingRepository>();

            string[] demofiles = Directory.GetFiles("./resources/", "*.sdfz");

            foreach (string demofile in demofiles) {
                _Logger.LogInformation($"adding demofile [demofile={demofile}]");

                byte[] bytes = await File.ReadAllBytesAsync(demofile);

                using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
                Result<BarMatch, string> data = await parser.Parse(Path.GetFileName(demofile), bytes, new DemofileParserOptions(), cts.Token);

                Assert.IsTrue(data.IsOk, $"got parse error for '{demofile}': {data.Error}");

                await processingRepo.Upsert(new BarMatchProcessing() {
                    GameID = data.Value.ID,
                    Priority = 1,
                    ReplayParsed = DateTime.UtcNow,
                    ReplayParsedMs = 0,
                    ReplayDownloaded = DateTime.UtcNow,
                    ReplayDownloadedMs = 0,
                });

                await processor.Process(data.Value, cts.Token);
            }

            TestLogger.DisableOutput = false;
            _Logger.LogInformation($"loaded matches");
        }

        /// <summary>
        ///     runs a string query by tokenizing, parsing the compiling the ast, and gets the results from the DB
        /// </summary>
        /// <param name="query">query string, expected to be valid</param>
        /// <returns>the list of matches that fulfil the query used in <paramref name="query"/></returns>
        private async Task<List<BarMatch>> _Query(string query) {
            Stopwatch timer = Stopwatch.StartNew();

            (IBarMatchDb db, ServiceProvider svs) = await _Get();

            SearchTokenizer tokenizer = svs.GetRequiredService<SearchTokenizer>();
            SearchAstBuilder astBuilder = svs.GetRequiredService<SearchAstBuilder>();

            SearchAst ast = astBuilder.Build(tokenizer.Tokenize(query));

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(15));
            Result<List<BarMatch>, string> results = await db.Search(ast, cts.Token);

            _Logger.LogInformation($"query complete [timer={timer.ElapsedMilliseconds}ms] [query={query}]");

            Assert.IsTrue(results.IsOk, $"results was not ok: {results.Error}");

            return results.Value;
        }

        /// <summary>
        ///     util test to dump all the matches loaded in the DB into stdout as JSON
        /// </summary>
        [TestMethod]
        public async Task Test_Util_Dump() {
            (IBarMatchDb db, ServiceProvider svs) = await _Get();

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(15));
            List<BarMatch> matches = await db.GetAll(cts.Token);

            ILogger<BarMatchDb_SearchQueryTest> logger = svs.GetRequiredService<ILogger<BarMatchDb_SearchQueryTest>>();

            IBarMatchBuilderUtil builder = svs.GetRequiredService<IBarMatchBuilderUtil>();

            List<BarMatch> built = [];
            foreach (BarMatch match in matches) {
                Result<Maybe<BarMatch>, string> ret = await builder.BuildMatch(match.ID, new IBarMatchBuilderUtil.BuildOptions() {
                    IncludeAiPlayers = true,
                    IncludeAllyTeams = true,
                    IncludePlayers = true,
                    IncludeTeams = true,
                }, null, cts.Token);

                built.Add(ret.Value.Get());
            }

            logger.LogInformation($"{JsonSerializer.SerializeToElement(built, new JsonSerializerOptions() {
                WriteIndented = true
            })}");
        }

        /// <summary>
        ///     ensure that a limit:0 throws
        /// </summary>
        [TestMethod]
        public async Task Test_Query_Limit0_Throws() {
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => {
                return _Query("limit:0");
            }, "limit must be greater than 0");
        }

        /// <summary>
        ///     ensure a limit:1 limits the results to 1
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task Test_Query_Limit1() {
            List<BarMatch> matches = await _Query("limit:1");

            Assert.AreEqual(1, matches.Count);
        }

        /// <summary>
        ///     ensure a limit:5 limits the results to 5
        /// </summary>
        [TestMethod]
        public async Task Test_Query_Limit5() {
            List<BarMatch> matches = await _Query("limit:5");

            Assert.AreEqual(5, matches.Count);
        }

        /// <summary>
        ///     ensure a limit:10 limits the results to 10
        /// </summary>
        [TestMethod]
        public async Task Test_Query_Limit10() {
            List<BarMatch> matches = await _Query("limit:10");

            Assert.AreEqual(10, matches.Count);
        }

        /// <summary>
        ///     ensure a limit:1000 gets all matches inserted
        /// </summary>
        [TestMethod]
        public async Task Test_Query_Limit1000() {
            List<BarMatch> matches = await _Query("limit:1000");

            Assert.AreEqual(GetDemofileCount(), matches.Count);
        }

        /// <summary>
        ///     ensure limit:1001 throws (over the max of 1000)
        /// </summary>
        [TestMethod]
        public async Task Test_Query_Limit1001_Throws() {
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => {
                return _Query("limit:1001");
            });
        }

        /// <summary>
        ///     ensure a sort:start_time works in asc dir
        /// </summary>
        [TestMethod]
        public async Task Test_Query_Sort_StartTime_Asc() {
            List<BarMatch> matches = await _Query("sort:start_time sort_dir:asc limit:1000");

            Assert.AreEqual(GetDemofileCount(), matches.Count);

            _Logger?.LogInformation($"start times: {string.Join(", ", matches.Select(iter => iter.StartTime.ToString("u")))}");

            DateTime previousStart = DateTime.MinValue;
            foreach (BarMatch match in matches) {
                Assert.IsTrue(match.StartTime >= previousStart, $"expected StartTime of {match.ID} ({match.StartTime:u}) to be higher than previous value {previousStart:u}");
                previousStart = match.StartTime;
            }
        }

        /// <summary>
        ///     ensure a sort:start_time works in desc dir
        /// </summary>
        [TestMethod]
        public async Task Test_Query_Sort_StartTime_Desc() {
            List<BarMatch> matches = await _Query("sort:start_time sort_dir:desc limit:1000");

            Assert.AreEqual(GetDemofileCount(), matches.Count);

            _Logger?.LogInformation($"start times: {string.Join(", ", matches.Select(iter => iter.StartTime.ToString("u")))}");

            DateTime previousStart = DateTime.MaxValue;
            foreach (BarMatch match in matches) {
                Assert.IsTrue(match.StartTime <= previousStart, $"expected StartTime of {match.ID} ({match.StartTime:u}) to be lower than previous value {previousStart:u}");
                previousStart = match.StartTime;
            }
        }

        /// <summary>
        ///     ensure a sort:duration works in desc dir
        /// </summary>
        [TestMethod]
        public async Task Test_Query_Sort_Duration_Asc() {
            List<BarMatch> matches = await _Query("sort:duration sort_dir:asc limit:1000");

            Assert.AreEqual(GetDemofileCount(), matches.Count);

            _Logger.LogInformation($"{string.Join(", ", matches.Select(iter => iter.DurationFrameCount))}");

            long duration = -1;
            foreach (BarMatch match in matches) {
                Assert.IsTrue(match.DurationFrameCount >= duration, $"expected duration of {match.ID} ({match.DurationFrameCount}) to be higher than previous value {duration}");
                duration = match.DurationFrameCount;
            }
        }

        /// <summary>
        ///     ensure a sort:duration works in desc dir
        /// </summary>
        [TestMethod]
        public async Task Test_Query_Sort_Duration_Desc() {
            List<BarMatch> matches = await _Query("sort:duration sort_dir:desc limit:1000");

            Assert.AreEqual(GetDemofileCount(), matches.Count);

            long duration = long.MaxValue;
            foreach (BarMatch match in matches) {
                Assert.IsTrue(match.DurationFrameCount <= duration, $"expected duration of {match.ID} ({match.DurationFrameCount}) to be lower than previous value {duration}");
                duration = match.DurationFrameCount;
            }
        }

        [TestMethod]
        public async Task Test_Query_PlayersOr() {
            List<BarMatch> matches = await _Query("player: cetme | fightwar");

            Assert.AreEqual(1, matches.Count);
        }

        [TestMethod]
        public async Task Test_Query_Grouped_Or() {
            List<BarMatch> matches = await _Query("{ player: cetme fightwar } | { map: supreme }");

            Assert.AreEqual(1, matches.Count);
        }

    }
}
