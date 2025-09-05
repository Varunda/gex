using gex.Services.Repositories;
using gex.Tests.Util;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Tests.Services.Repository {

    [TestClass]
    public class BarI18nRepositoryTest {

        private BarI18nRepository _Make => new BarI18nRepository(
            logger: new TestLogger<BarI18nRepository>(),
            githubRepository: new FsGithubDownloadRepository(new TestLogger<FsGithubDownloadRepository>()),
            cache: new NonCachingCache()
        );

        [TestMethod]
        public async Task I18n_Test() {
            BarI18nRepository i18n = _Make;

            CancellationTokenSource cts = new(TimeSpan.FromSeconds(1));

            Assert.AreEqual("Armada", await i18n.GetString("units", "units.factions.arm", cts.Token));
            Assert.AreEqual("%{name} Wreckage", await i18n.GetString("units", "units.dead", cts.Token));
            Assert.AreEqual("Archangel", await i18n.GetString("units", "units.names.armaak", cts.Token));
            Assert.AreEqual("Advanced Amphibious Anti-Air Bot", await i18n.GetString("units", "units.descriptions.armaak", cts.Token));

        }

        [TestMethod]
        public async Task I18n_GetKeysStartsWith() {
            BarI18nRepository i18n = _Make;

            CancellationTokenSource cts = new(TimeSpan.FromSeconds(1));
            List<KeyValuePair<string, string>> factionMatch = await i18n.GetKeysStartingWith("units", "units.factions.", cts.Token);
            Assert.AreEqual(3, factionMatch.Count);
            Assert.AreEqual("units.factions.arm", factionMatch[0].Key);
            Assert.AreEqual("Armada", factionMatch[0].Value);
            Assert.AreEqual("units.factions.cor", factionMatch[1].Key);
            Assert.AreEqual("Cortex", factionMatch[1].Value);
            Assert.AreEqual("units.factions.leg", factionMatch[2].Key);
            Assert.AreEqual("Legion", factionMatch[2].Value);
        }

        [TestMethod]
        public async Task I18n_GetMissingKey() {
            BarI18nRepository i18n = _Make;
            CancellationTokenSource cts = new(TimeSpan.FromSeconds(1));

            Assert.IsNull(await i18n.GetString("units", "asdfasdfasdfas", cts.Token));
        }

        [TestMethod]
        public async Task I18n_GetMissingFile() {
            BarI18nRepository i18n = _Make;
            CancellationTokenSource cts = new(TimeSpan.FromSeconds(1));

            Assert.IsNull(await i18n.GetString("asdf", "asdfasdfasdfas", cts.Token));
        }

    }
}
