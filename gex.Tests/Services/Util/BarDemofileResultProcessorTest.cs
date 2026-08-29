using gex.Common.Models;
using gex.Models.Db;
using gex.Services.Metrics;
using gex.Services.Parser;
using gex.Services.Queues;
using gex.Services.Repositories;
using gex.Services.Storage;
using gex.Services.Util;
using gex.Tests.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Tests.Services.Util {

    [TestClass]
    public class BarDemofileResultProcessorTest {

        private async Task<(BarDemofileResultProcessor, ServiceProvider)> _Get() {
            ServiceCollection services = await Service.Standard();
            services.AddSingleton<LuaCommandParser>();
            services.AddSingleton<BarDemofileParser>();
            services.AddSingleton<DemofileStorage>();
            services.AddGexQueueServices();
            services.AddGexMetrics();
            services.AddGexParsers();

            ServiceProvider svs = services.BuildServiceProvider();

            return (svs.GetRequiredService<BarDemofileResultProcessor>(), svs);
        }

        [DataTestMethod]
        [DataRow("test.sdfz")]
        [DataRow("test2.sdfz")]
        [DataRow("BAR105_2590_map_draw.sdfz")]
        [DataRow("2025.04.08_map_draw.sdfz")]
        [DataRow("2025.06.06_map_draw_test.sdfz")]
        [DataRow("BAR105_1821.sdfz")]
        [DataRow("2025.06.19_IndexOutOfRange.sdfz")]
        public async Task Test_Process_Bulk(string file) {
            (BarDemofileResultProcessor processor, ServiceProvider svs) = await _Get();

            using FileStream testInput = File.OpenRead($"./resources/{file}");
            using MemoryStream ms = new();
            await testInput.CopyToAsync(ms);

            byte[] input = ms.ToArray();

            BarDemofileParser parser = svs.GetRequiredService<BarDemofileParser>();
            Result<BarMatch, string> output = await parser.Parse("", input, new DemofileParserOptions() {
                IncludeMapDraws = true,
                IncludeCommands = true,
            }, CancellationToken.None);

            Assert.IsTrue(output.IsOk, $"error: {output.Error}");

            BarMatchProcessingRepository matchProcessingRepository = svs.GetRequiredService<BarMatchProcessingRepository>();
            await matchProcessingRepository.Upsert(new BarMatchProcessing() {
                GameID = output.Value.ID,
            });

            await processor.Process(output.Value, CancellationToken.None);

            BarMatchRepository matchRepository = svs.GetRequiredService<BarMatchRepository>();

            Result<Maybe<BarMatch>, string> ret = await matchRepository.BuildMatch(output.Value.ID, new BarMatchRepository.BuildOptions() {
                IncludeAllyTeams = true,
                IncludeChat = true,
                IncludeLabeledPings = true,
                IncludePlayerLeaves = true,
                IncludePlayers = true,
                IncludeSpectators = true,
                IncludeTeamDeaths = true,
                IncludeTeams = true,

                // these try to load the demofile from storage, which doesn't exist
                IncludeMapDraws = false,
                IncludeCommands = false,
                IncludeSelfDCommands = false,
            }, null, CancellationToken.None);

            Assert.IsTrue(ret.IsOk, $"error: {ret.Error}");
            Assert.IsTrue(ret.Value.Has());

            BarMatch match = ret.Value.Get();
            Assert.AreEqual(output.Value.ID, match.ID);
        }

        [TestMethod]
        public async Task Test_Process_Duel() {
            (BarDemofileResultProcessor processor, ServiceProvider svs) = await _Get();

            using FileStream testInput = File.OpenRead($"./resources/test3.sdfz");
            using MemoryStream ms = new();
            await testInput.CopyToAsync(ms);

            byte[] input = ms.ToArray();

            BarDemofileParser parser = svs.GetRequiredService<BarDemofileParser>();
            Result<BarMatch, string> output = await parser.Parse("", input, new DemofileParserOptions() {
                IncludeMapDraws = true,
                IncludeCommands = true,
            }, CancellationToken.None);

            Assert.IsTrue(output.IsOk, $"error: {output.Error}");

            BarMatchProcessingRepository matchProcessingRepository = svs.GetRequiredService<BarMatchProcessingRepository>();
            await matchProcessingRepository.Upsert(new BarMatchProcessing() {
                GameID = "c5d0cb673d1c101091ba9c25b84d7a69",
            });

            await processor.Process(output.Value, CancellationToken.None);

            BarMatchRepository matchRepository = svs.GetRequiredService<BarMatchRepository>();

            Result<Maybe<BarMatch>, string> ret = await matchRepository.BuildMatch("c5d0cb673d1c101091ba9c25b84d7a69", new BarMatchRepository.BuildOptions() {
                IncludeAllyTeams = true,
                IncludeChat = true,
                IncludeLabeledPings = true,
                IncludePlayerLeaves = true,
                IncludePlayers = true,
                IncludeSpectators = true,
                IncludeTeamDeaths = true,
                IncludeTeams = true,

                // these try to load the demofile from storage, which doesn't exist
                IncludeMapDraws = false,
                IncludeCommands = false,
                IncludeSelfDCommands = false,
            }, null, CancellationToken.None);

            Assert.IsTrue(ret.IsOk, $"error: {ret.Error}");
            Assert.IsTrue(ret.Value.Has());
            Assert.IsNotNull(ret.Value.Get());

            BarMatch match = ret.Value.Get();
            Assert.AreEqual("c5d0cb673d1c101091ba9c25b84d7a69", match.ID);
            Assert.AreEqual(29.66f, match.MaxOS);
            Assert.AreEqual(24.735f, match.AverageOS);
            Assert.AreEqual(19.81f, match.MinOS);

            Assert.AreEqual(1, match.Gamemode);
            Assert.AreEqual("2025.01.6", match.Engine);
            Assert.AreEqual("Isidis crack 1.1", match.Map);
            Assert.AreEqual(898000, match.DurationMs);
            Assert.AreEqual(23776, match.DurationFrameCount);

            Assert.AreEqual(0, match.AiPlayers.Count);
            Assert.AreEqual(2, match.AllyTeams.Count);
            Assert.AreEqual(2, match.Teams.Count);
            Assert.AreEqual(2, match.Players.Count);
            Assert.AreEqual(1, match.Spectators.Count);

            BarMatchAllyTeam? allyTeam0 = match.AllyTeams.FirstOrDefault(iter => iter.AllyTeamID == 0);
            Assert.IsNotNull(allyTeam0);
            Assert.AreEqual(0, allyTeam0.AllyTeamID);
            Assert.AreEqual(1, allyTeam0.PlayerCount);
            Assert.AreEqual(true, allyTeam0.Won);
            Assert.AreEqual(0.33f, allyTeam0.StartBox.Bottom);
            Assert.AreEqual(0.665f, allyTeam0.StartBox.Left);
            Assert.AreEqual(1.0f, allyTeam0.StartBox.Right);
            Assert.AreEqual(0.0f, allyTeam0.StartBox.Top);

            BarMatchAllyTeam? allyTeam1 = match.AllyTeams.FirstOrDefault(iter => iter.AllyTeamID == 1);
            Assert.IsNotNull(allyTeam1);
            Assert.AreEqual(1, allyTeam1.AllyTeamID);
            Assert.AreEqual(1, allyTeam1.PlayerCount);
            Assert.AreEqual(false, allyTeam1.Won);
            Assert.AreEqual(1.0f, allyTeam1.StartBox.Bottom);
            Assert.AreEqual(0f, allyTeam1.StartBox.Left);
            Assert.AreEqual(0.33f, allyTeam1.StartBox.Right);
            Assert.AreEqual(0.665f, allyTeam1.StartBox.Top);

            BarMatchTeam? team0 = match.Teams.FirstOrDefault(iter => iter.TeamID == 0);
            Assert.IsNotNull(team0);
            Assert.AreEqual(0, team0.TeamID);
            Assert.AreEqual("Cortex", team0.Faction);
            Assert.AreEqual(0, team0.TeamLeaderID);
            Assert.AreEqual(0, team0.AllyTeamID);
            Assert.AreEqual(0, team0.Handicap);
            Assert.AreEqual(737011, team0.Color);

            BarMatchTeam? team1 = match.Teams.FirstOrDefault(iter => iter.TeamID == 1);
            Assert.IsNotNull(team1);
            Assert.AreEqual(1, team1.TeamID);
            Assert.AreEqual("Cortex", team1.Faction);
            Assert.AreEqual(1, team1.TeamLeaderID);
            Assert.AreEqual(1, team1.AllyTeamID);
            Assert.AreEqual(0, team1.Handicap);
            Assert.AreEqual(16715781, team1.Color);

            BarMatchPlayer? player0 = match.Players.FirstOrDefault(iter => iter.PlayerID == 0);
            Assert.IsNotNull(player0);
            Assert.AreEqual(0, player0.PlayerID);
            Assert.AreEqual(0, player0.TeamID);
            Assert.AreEqual(0, player0.AllyTeamID);
            Assert.AreEqual("Victoria", player0.Name);
            Assert.AreEqual(29.66f, player0.Skill, 0.01);
            Assert.AreEqual(3.17f, player0.SkillUncertainty, 0.01);
            Assert.AreEqual(null, player0.CountryCode);

            BarMatchPlayer? player1 = match.Players.FirstOrDefault(iter => iter.PlayerID == 1);
            Assert.IsNotNull(player1);
            Assert.AreEqual(1, player1.PlayerID);
            Assert.AreEqual(1, player1.TeamID);
            Assert.AreEqual(1, player1.AllyTeamID);
            Assert.AreEqual("varunda", player1.Name);
            Assert.AreEqual(19.81f, player1.Skill, 0.01);
            Assert.AreEqual(4.27f, player1.SkillUncertainty, 0.01);
            Assert.AreEqual(null, player1.CountryCode);
        }

        [TestMethod]
        public async Task Test_Process_QuantumMode() {
            (BarDemofileResultProcessor processor, ServiceProvider svs) = await _Get();

            using FileStream testInput = File.OpenRead($"./resources/2026.07.04_QuantumMode.sdfz");
            using MemoryStream ms = new();
            await testInput.CopyToAsync(ms);

            byte[] input = ms.ToArray();

            BarDemofileParser parser = svs.GetRequiredService<BarDemofileParser>();
            Result<BarMatch, string> output = await parser.Parse("", input, new DemofileParserOptions() {
                IncludeMapDraws = true,
                IncludeCommands = true,
            }, CancellationToken.None);

            Assert.IsTrue(output.IsOk, $"error: {output.Error}");

            BarMatchProcessingRepository matchProcessingRepository = svs.GetRequiredService<BarMatchProcessingRepository>();
            await matchProcessingRepository.Upsert(new BarMatchProcessing() {
                GameID = "2144826a169387cee01499f2922c384b",
            });

            await processor.Process(output.Value, CancellationToken.None);

            BarMatchRepository matchRepository = svs.GetRequiredService<BarMatchRepository>();

            Result<Maybe<BarMatch>, string> ret = await matchRepository.BuildMatch("2144826a169387cee01499f2922c384b", new BarMatchRepository.BuildOptions() {
                IncludeAllyTeams = true,
                IncludeChat = true,
                IncludeLabeledPings = true,
                IncludePlayerLeaves = true,
                IncludePlayers = true,
                IncludeSpectators = true,
                IncludeTeamDeaths = true,
                IncludeTeams = true,

                // these try to load the demofile from storage, which doesn't exist
                IncludeMapDraws = false,
                IncludeCommands = false,
                IncludeSelfDCommands = false,
            }, null, CancellationToken.None);

            Assert.IsTrue(ret.IsOk, $"error: {ret.Error}");
            Assert.IsTrue(ret.Value.Has());
            Assert.IsNotNull(ret.Value);

            BarMatch match = ret.Value.Get();
            Assert.AreEqual("2144826a169387cee01499f2922c384b", match.ID);
            Assert.AreEqual(60.5f, match.MaxOS, 0.01);
            Assert.AreEqual(49.9625f, match.AverageOS, 0.01);
            Assert.AreEqual(33.19, match.MinOS, 0.01);

            Assert.AreEqual(2, match.Gamemode);
            Assert.AreEqual("2026.07.04", match.Engine);
            Assert.AreEqual("Otago 1.43", match.Map);
            Assert.AreEqual(1261000, match.DurationMs);
            Assert.AreEqual(35296, match.DurationFrameCount);

            Assert.AreEqual(0, match.AiPlayers.Count);
            Assert.AreEqual(2, match.AllyTeams.Count);
            Assert.AreEqual(2, match.Teams.Count);
            Assert.AreEqual(4, match.Players.Count);
            Assert.AreEqual(2, match.Spectators.Count);

            BarMatchAllyTeam? allyTeam0 = match.AllyTeams.FirstOrDefault(iter => iter.AllyTeamID == 0);
            Assert.IsNotNull(allyTeam0);
            Assert.AreEqual(0, allyTeam0.AllyTeamID);
            Assert.AreEqual(2, allyTeam0.PlayerCount);
            Assert.AreEqual(false, allyTeam0.Won);
            Assert.AreEqual(1f, allyTeam0.StartBox.Bottom);
            Assert.AreEqual(0f, allyTeam0.StartBox.Left);
            Assert.AreEqual(0.25f, allyTeam0.StartBox.Right);
            Assert.AreEqual(0.0f, allyTeam0.StartBox.Top);

            BarMatchAllyTeam? allyTeam1 = match.AllyTeams.FirstOrDefault(iter => iter.AllyTeamID == 1);
            Assert.IsNotNull(allyTeam1);
            Assert.AreEqual(1, allyTeam1.AllyTeamID);
            Assert.AreEqual(2, allyTeam1.PlayerCount);
            Assert.AreEqual(true, allyTeam1.Won);
            Assert.AreEqual(1f, allyTeam1.StartBox.Bottom);
            Assert.AreEqual(0.75f, allyTeam1.StartBox.Left);
            Assert.AreEqual(1f, allyTeam1.StartBox.Right);
            Assert.AreEqual(0f, allyTeam1.StartBox.Top);

            BarMatchTeam? team0 = match.Teams.FirstOrDefault(iter => iter.TeamID == 0);
            Assert.IsNotNull(team0);
            Assert.AreEqual(0, team0.TeamID);
            Assert.AreEqual("Cortex", team0.Faction);
            Assert.AreEqual(0, team0.TeamLeaderID);
            Assert.AreEqual(0, team0.AllyTeamID);
            Assert.AreEqual(0, team0.Handicap);
            Assert.AreEqual(737011, team0.Color);

            BarMatchTeam? team1 = match.Teams.FirstOrDefault(iter => iter.TeamID == 1);
            Assert.IsNotNull(team1);
            Assert.AreEqual(1, team1.TeamID);
            Assert.AreEqual("Cortex", team1.Faction);
            Assert.AreEqual(2, team1.TeamLeaderID);
            Assert.AreEqual(1, team1.AllyTeamID);
            Assert.AreEqual(0, team1.Handicap);
            Assert.AreEqual(16715781, team1.Color);

            BarMatchPlayer? player0 = match.Players.FirstOrDefault(iter => iter.PlayerID == 0);
            Assert.IsNotNull(player0);
            Assert.AreEqual(0, player0.PlayerID);
            Assert.AreEqual(0, player0.TeamID);
            Assert.AreEqual(0, player0.AllyTeamID);

            BarMatchPlayer? player1 = match.Players.FirstOrDefault(iter => iter.PlayerID == 1);
            Assert.IsNotNull(player1);
            Assert.AreEqual(1, player1.PlayerID);
            Assert.AreEqual(0, player1.TeamID);
            Assert.AreEqual(0, player1.AllyTeamID);

            BarMatchPlayer? player2 = match.Players.FirstOrDefault(iter => iter.PlayerID == 2);
            Assert.IsNotNull(player2);
            Assert.AreEqual(2, player2.PlayerID);
            Assert.AreEqual(1, player2.TeamID);
            Assert.AreEqual(1, player2.AllyTeamID);

            BarMatchPlayer? player3 = match.Players.FirstOrDefault(iter => iter.PlayerID == 3);
            Assert.IsNotNull(player3);
            Assert.AreEqual(3, player3.PlayerID);
            Assert.AreEqual(1, player3.TeamID);
            Assert.AreEqual(1, player3.AllyTeamID);
        }

    }
}
