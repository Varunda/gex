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
    public class BarMatchDbTest {

        private async Task<(IBarMatchDb, ServiceProvider)> _Get() {
            ServiceCollection services = await Service.Standard();

            ServiceProvider svs = services.BuildServiceProvider();

            return (svs.GetRequiredService<IBarMatchDb>(), svs);
        }

        [TestMethod]
        public async Task Test_GetByID() {
            (IBarMatchDb db, _) = await _Get();

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));

            await db.Insert(new BarMatch() {
                ID = "abc"
            }, cts.Token);

            BarMatch? match = await db.GetByID("abc", cts.Token);
            Assert.IsNotNull(match);
            Assert.AreEqual("abc", match.ID);
        }

        [TestMethod]
        public async Task Test_Search_Players() {
            (IBarMatchDb db, ServiceProvider svs) = await _Get();

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(15));

            IBarMatchTeamDb teamDb = svs.GetRequiredService<IBarMatchTeamDb>();
            IBarMatchPlayerDb playerDb = svs.GetRequiredService<IBarMatchPlayerDb>();

            await db.Insert(new BarMatch() {
                ID = "abc"
            }, cts.Token);

            await teamDb.Insert(new BarMatchTeam() {
                GameID = "abc",
                TeamID = 0,
                AllyTeamID = 0,
                Faction = "Cortex",
            }, cts.Token);

            await playerDb.Insert(new BarMatchPlayer() {
                GameID = "abc",
                TeamID = 0,
                AllyTeamID = 0,
                Name = "user1",
                PlayerID = 0,
                UserID = 0,
                Skill = 10
            });

            await teamDb.Insert(new BarMatchTeam() {
                GameID = "abc",
                TeamID = 1,
                AllyTeamID = 1,
                Faction = "Legion"
            }, cts.Token);

            await playerDb.Insert(new BarMatchPlayer() {
                GameID = "abc",
                TeamID = 1,
                AllyTeamID = 1,
                Name = "user2",
                PlayerID = 1,
                UserID = 1,
                Skill = 20
            });

            // no teams played armada, expect empty
            List<BarMatch> result1 = await db.Search(new BarMatchSearchParameters() {
                Players = [
                    new SearchPlayer() {
                        Faction = "Armada"
                    }
                ]
            }, 0, 10, null, cts.Token);

            Assert.AreEqual(0, result1.Count, $"expected 0 games with Armada players");

            // search Cortex, but set the MinOS too high for 'user1' to be included
            List<BarMatch> result2 = await db.Search(new BarMatchSearchParameters() {
                Players = [
                    new SearchPlayer() {
                        Faction = "Cortex",
                        MinOS = 20
                    }
                ]
            }, 0, 10, null, cts.Token);
            Assert.AreEqual(0, result2.Count, $"expected 0 games with Cortex and MinOS > 20");

            List<BarMatch> result3 = await db.Search(new BarMatchSearchParameters() {
                Players = [
                    new SearchPlayer() {
                        Faction = "Cortex",
                        MinOS = 5
                    }
                ]
            }, 0, 10, null, cts.Token);
            Assert.AreEqual(1, result3.Count, $"expected 1 game with Cortex and MinOS > 5");
        }


    }
}
