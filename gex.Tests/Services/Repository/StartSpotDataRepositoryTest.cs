using gex.Models.Map;
using gex.Services.Db;
using gex.Services.Db.Map;
using gex.Services.Repositories;
using gex.Tests.Util;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Tests.Services.Repository {

    [TestClass]
    public class StartSpotDataRepositoryTest {

        private async Task<StartSpotDataRepository> _Make(IDbHelper? dbHelper = null) {
            dbHelper ??= await DbUtil.Create();

            ILoggerFactory loggerFactory = new LoggerFactory();

            StartSpotDataRepository repo = new(
                logger: new TestLogger<StartSpotDataRepository>(),
                dataDb: new StartSpotDataDb(loggerFactory, dbHelper),
                positionDb: new StartSpotPositionDb(loggerFactory, dbHelper),
                configurationDb: new StartSpotConfigurationDb(loggerFactory, dbHelper),
                sideDb: new StartSpotSideDb(loggerFactory, dbHelper),
                sideStartDb: new StartSpotSideStartDb(loggerFactory, dbHelper),
                overrideDb: new StartSpotSideStartRoleOverrideDb(loggerFactory, dbHelper),
                cache: new NonCachingCache()
            );

            return repo;
        }

        [TestMethod]
        public async Task Test_InsertAndGet() {

            StartSpotDataRepository repo = await _Make();

            StartSpotData d1 = StartSpotDataUtil.Create();
            StartSpotData r1 = await repo.Insert(d1, CancellationToken.None);

            StartSpotData? l1 = await repo.GetLatestByMapFilename("roseta_v1.4", CancellationToken.None);
            Assert.IsNotNull(l1);
            Assert.IsTrue(d1 == l1);

            Assert.AreEqual(1, r1.Version);

            bool threw = false;
            try {
                StartSpotData dtemp = StartSpotDataUtil.Create();
                await repo.Insert(dtemp, CancellationToken.None);
            } catch (Exception ex) {
                Assert.IsTrue(ex.Message.StartsWith("the newly inserted data is the same as the latest version"), $"wrong exception message: {ex.Message}");
                threw = true;
            }

            Assert.IsTrue(threw, $"insert for duplicate start spot data did not throw");

            StartSpotData d2 = StartSpotDataUtil.Create2();
            Assert.AreNotEqual(d1, d2);

            StartSpotData r2 = await repo.Insert(d2, CancellationToken.None);
            Assert.AreEqual(2, r2.Version);

            StartSpotData? l2 = await repo.GetLatestByMapFilename("roseta_v1.4", CancellationToken.None);
            Assert.IsNotNull(l2);
            Assert.AreEqual(d2, l2);
        }

        [TestMethod]
        public async Task Test_InsertAndGetWithOverride() {
            IDbHelper dbHelper = await DbUtil.Create();

            StartSpotDataRepository repo = await _Make(dbHelper);

            StartSpotData d1 = StartSpotDataUtil.Create();
            await repo.Insert(d1, CancellationToken.None);

            StartSpotSideStartRoleOverrideDb overrideDb = new(new LoggerFactory(), dbHelper);
            await overrideDb.Upsert(new StartSpotSideStartRoleOverride() {
                MapFilename = d1.MapFilename,
                Version = d1.Version,
                Position = "P1",
                Role = "override"
            }, CancellationToken.None);

            List<StartSpotSideStartRoleOverride> @override = await overrideDb.GetLatestByMapFilename("roseta_v1.4", CancellationToken.None);
            Assert.AreEqual(1, @override.Count);

            StartSpotData? r1 = await repo.GetLatestByMapFilename("roseta_v1.4", CancellationToken.None);
            Assert.IsNotNull(r1);

            Assert.AreEqual("override", r1.Configurations[0].Sides[0].Starts[0].Role);
        }
        
    }

}
