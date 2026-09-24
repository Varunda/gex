using gex.Common.Models.Match;
using gex.Common.Services.Db.Match;
using gex.Tests.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Tests.Services.Db {

    [TestClass]
    public class BarMatchTeamDbTest {

        private async Task<(IBarMatchTeamDb, ServiceProvider)> _Get() {
            ServiceCollection services = await Service.Standard();

            ServiceProvider svs = services.BuildServiceProvider();

            return (svs.GetRequiredService<IBarMatchTeamDb>(), svs);
        }

        [TestMethod]
        public async Task Test_InsertAndGet() {
            (IBarMatchTeamDb db, ServiceProvider svs) = await _Get();

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(10));
            await db.Insert(new BarMatchTeam() {
                GameID = "abc",
                AllyTeamID = 0,
                Color = 1,
                Faction = "Armada",
                Handicap = 10,
                OpeningLabUnitDefinitionName = "armlab",
                StartingPosition = new System.Numerics.Vector3() { X = 10, Y = 11, Z = 12 },
                StartSpot = "P1",
                StartSpotLabel = "air",
                TeamID = 0,
                TeamLeaderID = 0
            }, cts.Token);

            await db.Insert(new BarMatchTeam() {
                GameID = "abc",
                AllyTeamID = 1,
                Color = 100,
                Faction = "Cortex",
                Handicap = 50,
                OpeningLabUnitDefinitionName = "corsy",
                StartingPosition = new System.Numerics.Vector3() { X = 20, Y = 21, Z = 22 },
                StartSpot = "P2",
                StartSpotLabel = "front",
                TeamID = 1,
                TeamLeaderID = 1
            }, cts.Token);

            List<BarMatchTeam> teams = await db.GetByGameID("abc", cts.Token);
            Assert.AreEqual(2, teams.Count);

            BarMatchTeam? team0 = teams.FirstOrDefault(iter => iter.TeamID == 0);
            Assert.IsNotNull(team0);

            Assert.AreEqual(0, team0.TeamID);
            Assert.AreEqual(0, team0.AllyTeamID);
            Assert.AreEqual(1, team0.Color);
            Assert.AreEqual("Armada", team0.Faction);
            Assert.AreEqual(10, team0.Handicap);
            Assert.AreEqual("armlab", team0.OpeningLabUnitDefinitionName);
            Assert.AreEqual(10f, team0.StartingPosition.X);
            Assert.AreEqual(11f, team0.StartingPosition.Y);
            Assert.AreEqual(12f, team0.StartingPosition.Z);
            Assert.AreEqual("P1", team0.StartSpot);
            Assert.AreEqual("air", team0.StartSpotLabel);
            Assert.AreEqual(0, team0.TeamLeaderID);

            BarMatchTeam? team1 = teams.FirstOrDefault(iter => iter.TeamID == 1);
            Assert.IsNotNull(team1);
            Assert.AreEqual(1, team1.TeamID);
            Assert.AreEqual(1, team1.AllyTeamID);
            Assert.AreEqual(100, team1.Color);
            Assert.AreEqual("Cortex", team1.Faction);
            Assert.AreEqual(50, team1.Handicap);
            Assert.AreEqual("corsy", team1.OpeningLabUnitDefinitionName);
            Assert.AreEqual(20f, team1.StartingPosition.X);
            Assert.AreEqual(21f, team1.StartingPosition.Y);
            Assert.AreEqual(22f, team1.StartingPosition.Z);
            Assert.AreEqual("P2", team1.StartSpot);
            Assert.AreEqual("front", team1.StartSpotLabel);
            Assert.AreEqual(1, team1.TeamLeaderID);
        }

    }
}
