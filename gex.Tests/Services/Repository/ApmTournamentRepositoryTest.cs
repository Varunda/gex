using gex.Common.Code.ExtensionMethods;
using gex.Common.Models;
using gex.Models.Apm;
using gex.Models.Options;
using gex.Services.BarApi;
using gex.Services.Repositories;
using gex.Tests.Util;
using Microsoft.Extensions.Options;
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
    public class ApmTournamentRepositoryTest {

        private static ApmTournamentRepository? _Api = null;
        private static async Task<ApmTournamentRepository> _Make() {
            if (_Api == null) {
                Assert.IsTrue(File.Exists("secrets.json"), $"missing secrets.json");

                string s = await File.ReadAllTextAsync("secrets.json");
                JsonElement json = JsonSerializer.Deserialize<JsonElement>(s);

                JsonElement? apmWebsite = json.GetChild("ApmWebsite");
                Assert.IsNotNull(apmWebsite, $"missing .ApmWebsite from {json}");

                bool enabled = apmWebsite.Value.GetRequiredChild("Enabled").GetBoolean();
                Assert.IsTrue(enabled, $".ApmWebsite.Enabled is not true");

                string? apiKey = apmWebsite.Value.GetRequiredChild("ApiKey").GetString();
                Assert.IsNotNull(apiKey, $".ApmWebsite.ApiKey is null");

                string? apiUrl = apmWebsite.Value.GetRequiredChild("ApiUrl").GetString();
                Assert.IsNotNull(apiUrl, $".ApmWebsite.ApiUrl is null");

                _Api = new ApmTournamentRepository(
                    new TestLogger<ApmTournamentRepository>(),
                    new ApmWebsiteApi(new TestLogger<ApmWebsiteApi>(), Options.Create<ApmWebsiteOptions>(new ApmWebsiteOptions() {
                        Enabled = enabled,
                        ApiKey = apiKey!,
                        ApiUrl = apiUrl!,
                    })),
                    new NonCachingCache()
                );
            }

            return _Api;
        }

        [TestMethod]
        public async Task Test_GetAll() {
            ApmTournamentRepository repo = await _Make();

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));
            Result<List<ApmTournamentEntry>, string> ret = await repo.GetEntries(cts.Token);

            Assert.IsTrue(ret.IsOk, $"got error: {ret.Error}");
        }

        [TestMethod]
        public async Task Test_GetInfo() {
            ApmTournamentRepository repo = await _Make();

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));
            Result<List<ApmTournamentEntry>, string> ret = await repo.GetEntries(cts.Token);
            Assert.IsTrue(ret.IsOk, $"got error: {ret.Error}");
            Assert.IsTrue(ret.Value.Count > 0, $"needed at least 1 entry to get info of");

            Result<ApmTournamentInfo, string> info = await repo.GetTournamentInfo(ret.Value.ElementAt(0).ID, cts.Token);
            Assert.IsTrue(info.IsOk, $"got error: {info.Error}");
        }

    }
}
