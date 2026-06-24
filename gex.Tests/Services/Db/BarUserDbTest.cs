using gex.Models.UserStats;
using gex.Services.Db.UserStats;
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
    public class BarUserDbTest {

        private async Task<(BarUserDb, ServiceProvider)> _Get() {
            ServiceCollection services = await Service.Standard();

            ServiceProvider svs = services.BuildServiceProvider();

            return (svs.GetRequiredService<BarUserDb>(), svs);
        }

        [TestMethod("ensure calling Upsert() with a null country_code does not overwrite one that already exists")]
        public async Task Test_InsertThenUpsertWithNullCountryCode() {
            (BarUserDb db, _) = await _Get();

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));

            BarUser? user = await db.GetByID(1, cts.Token);
            Assert.IsNull(user);

            await db.Upsert(1, new BarUser() {
                UserID = 1,
                Username = "test",
                CountryCode = null,
                LastUpdated = DateTime.UtcNow,
            }, cts.Token);

            user = await db.GetByID(1, cts.Token);
            Assert.IsNotNull(user);
            Assert.AreEqual(1, user.UserID);
            Assert.AreEqual("test", user.Username);
            Assert.IsNull(user.CountryCode);

            await db.Upsert(1, new BarUser() {
                UserID = 1,
                Username = "test",
                CountryCode = "moon",
                LastUpdated = DateTime.UtcNow,
            }, cts.Token);

            user = await db.GetByID(1, cts.Token);
            Assert.IsNotNull(user);
            Assert.AreEqual(1, user.UserID);
            Assert.AreEqual("test", user.Username);
            Assert.AreEqual("moon", user.CountryCode);

            await db.Upsert(1, new BarUser() {
                UserID = 1,
                Username = "test",
                CountryCode = null,
                LastUpdated = DateTime.UtcNow,
            }, cts.Token);

            user = await db.GetByID(1, cts.Token);
            Assert.IsNotNull(user);
            Assert.AreEqual(1, user.UserID);
            Assert.AreEqual("test", user.Username);
            Assert.AreEqual("moon", user.CountryCode);
        }

    }
}
