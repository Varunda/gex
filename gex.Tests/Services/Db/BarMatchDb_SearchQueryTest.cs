using gex.Common.Code.Constants;
using gex.Common.Code.Exceptions;
using gex.Common.Code.ExtensionMethods;
using gex.Common.Models;
using gex.Common.Models.Map;
using gex.Common.Models.Match;
using gex.Common.Models.Search;
using gex.Common.Models.User;
using gex.Common.Services.Db;
using gex.Common.Services.Db.Match;
using gex.Common.Services.Parser;
using gex.Common.Services.Repositories;
using gex.Common.Services.Repository;
using gex.Common.Services.Util;
using gex.Services.Search;
using gex.Services.Util;
using gex.Tests.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data.Common;
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
            TestLogger.DisableOutput = false;

            IDbHelper helper = svs.GetRequiredService<IDbHelper>();
            await DbUtil.TruncateTables(helper);

            _Logger.LogInformation($"loading maps...");

            string barMapSql = await File.ReadAllTextAsync("./resources/sql/bar_map.sql");

            using DbConnection conn = helper.Connection();
            using DbCommand cmd = await helper.Command(conn, barMapSql);

            await cmd.ExecuteNonQueryAsync();

            BarMapRepository mapRepo = svs.GetRequiredService<BarMapRepository>();
            List<BarMap> maps = await mapRepo.GetAll(CancellationToken.None);
            _Logger.LogInformation($"loaded maps ({maps.Count})");

            TestLogger.DisableOutput = true;
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

        [ClassCleanup]
        public static async Task Teardown() {
            (IBarMatchDb db, ServiceProvider svs) = await _Get();
            await DbUtil.TruncateTables(svs.GetRequiredService<IDbHelper>());
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
            await Assert.ThrowsExceptionAsync<SearchAstArgumentException>(() => {
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
            await Assert.ThrowsExceptionAsync<SearchAstArgumentException>(() => {
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

        /// <summary>
        ///     test to find all games of a player
        /// </summary>
        [TestMethod]
        public async Task Test_Query_PlayersFindAll() {
            List<BarMatch> matches = await _Query("player: varunda");

            Assert.AreEqual(5, matches.Count);
        }

        /// <summary>
        ///     test to find all games of a player that are with another player
        /// </summary>
        [TestMethod]
        public async Task Test_Query_PlayersFindAll_IncludeOtherPlayer() {
            List<BarMatch> matches = await _Query("player: varunda victoria");
            Assert.AreEqual(4, matches.Count);
        }

        /// <summary>
        ///     test to find all games of a player that DONT contain another player
        /// </summary>
        [TestMethod]
        public async Task Test_Query_PlayersFindAll_ExcludeOtherPlayer() {
            List<BarMatch> matches = await _Query("player: varunda !victoria");
            Assert.AreEqual(1, matches.Count);
        }

        /// <summary>
        ///     test to find all games of a player that aren't played on another map
        /// </summary>
        [TestMethod]
        public async Task Test_Query_PlayersFindAll_ExcludeMap() {
            List<BarMatch> matches = await _Query("player: varunda map: !pinewood");
            Assert.AreEqual(4, matches.Count);
        }

        [TestMethod]
        public async Task Test_Query_Grouped_Or() {
            List<BarMatch> matches = await _Query("{ player: cetme fightwar } | { map: gasbag }");
            Assert.AreEqual(2, matches.Count); // the 1 gasbag game, and the one game with fightwar in it
        }

        [TestMethod]
        public async Task Test_Query_Player_NoMatch() {
            List<BarMatch> matches = await _Query("player: doesnotexistinthisdataset");
            Assert.AreEqual(0, matches.Count);
        }

        [TestMethod]
        public async Task Test_Query_Map_NoMatch() {
            List<BarMatch> matches = await _Query("map: doesnotexistindataset");
            Assert.AreEqual(0, matches.Count);
        }

        [TestMethod]
        public async Task Test_Query_Gamemode_NotDuel() {
            List<BarMatch> matches = await _Query("gamemode:!duel");

            foreach (BarMatch match in matches) {
                Assert.AreNotEqual(BarGamemode.DUEL, match.Gamemode, $"expected non-DUEL gamemode, got {match.Gamemode}");
            }
        }

        [TestMethod]
        public async Task Test_Query_Player_CurrentName() {
            BarUserRepository userRepo = _ServiceProvier!.GetRequiredService<BarUserRepository>();
            List<UserSearchResult> users = await userRepo.SearchByName("[Blahaj]varunda", false, CancellationToken.None);

            Assert.IsTrue(users.Count > 0, $"expected at least 1 user searched with '[Blahaj]varunda' to exist");
            HashSet<long> userIDs = new(users.Select(iter => iter.UserID));

            List<BarMatch> matches = await _Query("player(current_name:[Blahaj]varunda)");

            foreach (BarMatch match in matches) {
                HashSet<long> matchUsers = new(match.Players.Select(iter => iter.UserID));

                bool found = false;
                foreach (long userID in userIDs) {
                    found = matchUsers.Contains(userID);
                    if (found == true) {
                        break;
                    }
                }

                Assert.IsTrue(found, $"failed to find any user with name of varunda in game");
            }
        }

        [TestMethod]
        public async Task Test_Query_Player_GameName() {
            List<BarMatch> matches = await _Query("player(game_name:varunda)");

            foreach (BarMatch match in matches) {
                Assert.IsNotNull(match.Players.FirstOrDefault(iter => iter.Name.Contains("varunda", StringComparison.OrdinalIgnoreCase)),
                    $"missing user with name varunda from {match.ID}: {string.Join(", ", match.Players.Select(i => i.Name))}");
            }
        }

        [TestMethod]
        public async Task Test_Query_Player_UserID() {
            List<BarMatch> matches = await _Query("player(user_id:146777)");

            Assert.IsTrue(matches.Count > 0, $"expected at least 1 match with user ID 146777");
            foreach (BarMatch match in matches) {
                Assert.IsNotNull(match.Players.FirstOrDefault(iter => iter.UserID == 146777),
                    $"missing user with ID 146777 from {match.ID}: {string.Join(", ", match.Players.Select(i => i.UserID))}");
            }
        }

        [TestMethod]
        public async Task Test_Query_Duration_GreaterThan() {
            List<BarMatch> matches = await _Query("duration:>5m");

            Assert.IsTrue(matches.Count > 0, $"expected at least 1 match over 5 minutes");
            foreach (BarMatch match in matches) {
                Assert.IsTrue(match.DurationMs > (1000 * 60 * 5), $"match was not over 5 minutes ({match.DurationMs})");
            }
        }

        [TestMethod]
        public async Task Test_Query_DurationFrames_GreaterThan() {
            List<BarMatch> matches = await _Query("duration_frames:>9000");

            Assert.IsTrue(matches.Count > 0, $"expected at least 1 match over 5 minutes (of frames)");
            foreach (BarMatch match in matches) {
                Assert.IsTrue(match.DurationFrameCount > (30 * 60 * 5), $"match was not over 5 minutes ({match.DurationFrameCount})");
            }
        }

        [TestMethod]
        public async Task Test_Query_DateTime_MatchStartTime_JustDate() {
            List<BarMatch> matches = await _Query("start_time>2026-01-01");

            Assert.IsTrue(matches.Count > 0, $"expected at least 1 match after 2026-01-01");
            foreach (BarMatch match in matches) {
                Assert.IsTrue(match.StartTime > DateTime.Parse("2026-01-01"), $"match was not after 2026-01-01");
            }
        }

        [TestMethod]
        public async Task Test_Query_DateTime_MatchStartTime_WithTime() {
            List<BarMatch> matches = await _Query("start_time>\"2026-01-01T00:00\"");

            Assert.IsTrue(matches.Count > 0, $"expected at least 1 match after 2026-01-01");
            foreach (BarMatch match in matches) {
                Assert.IsTrue(match.StartTime > DateTime.Parse("2026-01-01"), $"match was not after 2026-01-01");
            }
        }

        [TestMethod]
        public async Task Test_Query_RankedTrue() {
            List<BarMatch> matches = await _Query("ranked:true");

            Assert.IsTrue(matches.Count > 0, $"expected at least 1 match");
            foreach (BarMatch match in matches) {
                Assert.IsTrue("1" == match.GameSettings.GetString("ranked_game", ""), $"game was not ranked: {match.GameSettings.GetString("ranked_game", "")}");
            }
        }

        [TestMethod]
        public async Task Test_Query_ProcessedFieldsTrue() {
            List<BarMatch> matches = await _Query("downloaded:true");
            Assert.IsTrue(matches.Count > 0, $"expected at least 1 match for downloaded"); // all games are parsed

            matches = await _Query("parsed:true");
            Assert.IsTrue(matches.Count > 0, $"expected at least 1 match"); // all games are parsed

            matches = await _Query("replayed:false");
            Assert.IsTrue(matches.Count > 0, $"expected at least 1 match for not replayed"); 

            matches = await _Query("actions_parsed:false");
            Assert.IsTrue(matches.Count > 0, $"expected at least 1 match for not actions replayed"); 
        }

        [TestMethod]
        public async Task Test_Query_ParsedFalse() {
            List<BarMatch> matches = await _Query("parsed:false");
            Assert.AreEqual(0, matches.Count);
        }

        #region error test

        [TestMethod]
        public async Task Test_Error_Format_Bool() {
            await Assert.ThrowsExceptionAsync<FormatException>(() => {
                return _Query("ranked=10");
            });
        }

        [TestMethod]
        public async Task Test_Error_Format_Int() {
            await Assert.ThrowsExceptionAsync<FormatException>(() => {
                return _Query("player_count:howdy");
            });
        }

        [TestMethod]
        public async Task Test_Error_Format_Long() {
            await Assert.ThrowsExceptionAsync<FormatException>(() => {
                return _Query("duration_frame_count:howdy");
            });
        }

        [TestMethod]
        public async Task Test_Error_Format_Double() {
            await Assert.ThrowsExceptionAsync<FormatException>(() => {
                return _Query("average_os:howdy");
            });
        }

        [TestMethod]
        public async Task Test_Error_Format_StringDateTime() {
            await Assert.ThrowsExceptionAsync<FormatException>(() => {
                return _Query("start_time:howdy");
            });
        }

        [TestMethod]
        public async Task Test_Error_Format_StringTimeSpan() {
            await Assert.ThrowsExceptionAsync<FormatException>(() => {
                return _Query("duration_time:howdy");
            });
        }

        #endregion 

        #region helper functions

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // helper functions
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

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
            _Logger.LogInformation($"parsed ast: {ast.Print()}");
            _PrintAst(ast);

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(15));
            Result<List<BarMatch>, string> results = await db.Search(ast, cts.Token);

            _Logger.LogInformation($"query complete [timer={timer.ElapsedMilliseconds}ms] [query={query}]");

            Assert.IsTrue(results.IsOk, $"results was not ok: {results.Error}");

            _Logger.LogInformation($"matches found ({results.Value.Count}):");

            List<BarMatch> built = [];

            IBarMatchBuilderUtil builder = svs.GetRequiredService<IBarMatchBuilderUtil>();
            foreach (BarMatch iter in results.Value) {
                Result<Maybe<BarMatch>, string> ret = await builder.BuildMatch(iter.ID, new IBarMatchBuilderUtil.BuildOptions() {
                    IncludeAiPlayers = true,
                    IncludeAllyTeams = true,
                    IncludePlayers = true,
                    IncludeTeams = true,
                }, null, cts.Token);

                Assert.IsTrue(ret.IsOk, $"got error built match {iter.ID}: {ret.Error}");
                Assert.IsTrue(ret.Value.Has(), $"Maybe returned null for {iter.ID}");

                BarMatch match = ret.Value.Get();

                built.Add(match);

                Console.WriteLine($"game {match.ID} - {match.Map} - {match.StartTime:u}");

                foreach (BarMatchAllyTeam allyTeam in match.AllyTeams) {

                    List<BarMatchTeam> teams = match.Teams.Where(iter => iter.AllyTeamID == allyTeam.AllyTeamID).ToList();

                    if (teams.Count == 1) {
                        List<BarMatchPlayer> players = match.Players.Where(iter => iter.TeamID == teams[0].TeamID).ToList();
                        Console.WriteLine($"\t{allyTeam.AllyTeamID} - {string.Join(" & ", players.Select(iter => iter.Name))}: {teams[0].Faction}");
                    } else {
                        Console.WriteLine($"\t{allyTeam.AllyTeamID} - ");
                        foreach (BarMatchTeam team in teams) {
                            List<BarMatchPlayer> players = match.Players.Where(iter => iter.TeamID == team.TeamID).ToList();
                            Console.WriteLine($"\t\tTeam {team.TeamID} - {string.Join(" & ", players.Select(iter => iter.Name))}: {team.Faction}");
                        }
                    }
                }

                Console.WriteLine("");
            }

            return built;
        }

        private void _PrintAst(SearchAst ast) {
            _PrintNode(ast.Root);
        }

        private void _PrintNode(SearchNode node) {
            Console.WriteLine($"{new string('\t', node.Depth + 1)}{node.Type} ({node.Token.Type}) {node.Token.Value}");

            foreach (SearchNode child in node.Children) {
                _PrintNode(child);
            }
        }

        #endregion

    }
}
