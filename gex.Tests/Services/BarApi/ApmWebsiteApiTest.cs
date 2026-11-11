using gex.Common.Code.ExtensionMethods;
using gex.Common.Models;
using gex.Models.Apm;
using gex.Models.Options;
using gex.Services.BarApi;
using gex.Tests.Util;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Tests.Services.BarApi {

    [TestClass]
    public class ApmWebsiteApiTest {

        private static ApmWebsiteApi? _Api = null;
        private static async Task<ApmWebsiteApi> _Make() {
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

                _Api = new(new TestLogger<ApmWebsiteApi>(), Options.Create<ApmWebsiteOptions>(new ApmWebsiteOptions() {
                    Enabled = enabled,
                    ApiKey = apiKey!,
                    ApiUrl = apiUrl!,
                }));
            }

            return _Api;
        }

        [TestMethod]
        public async Task Test_GetTournamentEntires() {
            ApmWebsiteApi api = await _Make();

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));
            Result<List<ApmTournamentEntry>, string> ret = await api.GetEntries(cts.Token);

            Assert.IsTrue(ret.IsOk, $"got error: {ret.Error}");
        }

        [TestMethod]
        public async Task Test_GetTournamentInfo() {
            ApmWebsiteApi api = await _Make();

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));
            Result<List<ApmTournamentEntry>, string> ret = await api.GetEntries(cts.Token);

            Assert.IsTrue(ret.IsOk, $"got error: {ret.Error}");

            Assert.IsTrue(ret.Value.Count > 0, $"needing at least 1 tournament to test getting tournament info");

            Result<ApmTournamentInfo, string> info = await api.GetTournamentInfo(ret.Value[0].Slug, cts.Token);
            Assert.IsTrue(info.IsOk, $"got error: {info.Error}");

            Console.WriteLine($"got tournament {ret.Value[0].Slug}");
        }

        [TestMethod]
        public async Task Test_Error_GetMissingTournament() {
            ApmWebsiteApi api = await _Make();

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));
            Result<ApmTournamentInfo, string> ret = await api.GetTournamentInfo($"does-not-exist-{Guid.NewGuid()}", cts.Token);
            Assert.IsFalse(ret.IsOk, $"expected an error response for a non-existing tournament");
            Assert.AreEqual("got bad response NotFound: Tournament not found.", ret.Error);
        }

        [TestMethod]
        public async Task Test_Error_GetInvalidEntryPage() {
            ApmWebsiteApi api = await _Make();

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));
            Result<List<ApmTournamentEntry>, string> ret = await api.GetEntries(0, cts.Token);
            Assert.IsFalse(ret.IsOk, $"expected an error response for a bad page number");
            Assert.AreEqual("page must be at least 1", ret.Error);
        }

        /// <summary>
        ///     don't be an asshole and run this without letting admins of the APM website know
        /// </summary>
        /// <returns></returns>
        //[TestMethod]
        [Timeout(1000 * 60 * 5)]
        public async Task Test_Error_TripRateLimit() {
            ApmWebsiteApi api = await _Make();

            Task[] tasks = new Task[125];

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(120));
            for (int i = 0; i < tasks.Length; ++i) {
                tasks[i] = api.GetTournamentInfo($"does-not-exist-{Guid.NewGuid()}", cts.Token);
            }

            await Task.WhenAll(tasks);
        }

    }
}
