using gex.Common.Code.ExtensionMethods;
using gex.Common.Models;
using gex.Common.Models.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace gex.Common.Services.Util {

    public class CovenVersionUtil {

        private readonly ILogger<CovenVersionUtil> _Logger;

        private readonly IMemoryCache? _Cache;
        private const string CACHE_KEY_LATEST_VERSION = "gex.Coven.Version.Latest";

        private static readonly HttpClient _Http = new();

        static CovenVersionUtil() {
            _Http.DefaultRequestHeaders.UserAgent.TryParseAdd("gex.Coven/0.1");
        }

        public CovenVersionUtil(ILogger<CovenVersionUtil> logger,
            IMemoryCache? cache = null) {

            _Logger = logger;
            _Cache = cache;
        }

        public string? GetCurrentVersion() {
            FileVersionInfo? fvi = FileVersionInfo.GetVersionInfo(Path.Join(AppContext.BaseDirectory, "gex.Coven" + (OperatingSystem.IsWindows() ? ".exe" : "")));
            return $"coven-{fvi?.FileVersion}";
        }

        /// <summary>
        ///     get the latest <see cref="CovenVersion"/> available on the server
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<Result<CovenVersion, string>> GetLatest(CovenRepositoryOptions repo, CancellationToken cancel) {
            if (_Cache?.TryGetValue(CACHE_KEY_LATEST_VERSION, out CovenVersion? latestVersion) == true && latestVersion != null) {
                return latestVersion;
            }

            if (string.IsNullOrWhiteSpace(repo.Instance)
                || string.IsNullOrEmpty(repo.RepositoryOwner)
                || string.IsNullOrWhiteSpace(repo.RepositoryName)) {

                return $"missing repository info [instancer={repo.Instance}] [owner={repo.RepositoryOwner}] [name={repo.RepositoryName}]";
            }

            string url = $"{repo.Instance}/api/v1/repos/{repo.RepositoryOwner}/{repo.RepositoryName}/releases";
            Result<JsonElement, string> response = await _Http.GetJsonAsync(url, cancel);
            if (response.IsOk == false) {
                _Logger.LogWarning($"failed to get response [url={url}] [error={response.Error}]");
                return $"failed to load response: {response.Error}";
            }

            if (response.Value.ValueKind != JsonValueKind.Array) {
                return $"expected response to be an array [kind={response.Value.ValueKind}]";
            }

            List<CovenVersion> versions = [];

            foreach (JsonElement iter in response.Value.EnumerateArray()) {
                string name = iter.GetRequiredString("tag_name");
                if (name.StartsWith("coven") == false) {
                    continue;
                }

                DateTime publishedAt = DateTime.Parse(iter.GetRequiredString("published_at"));

                JsonElement assets = iter.GetRequiredChild("assets");
                if (assets.ValueKind != JsonValueKind.Array) {
                    return $"expected response.assets to be an array [kind={assets.ValueKind}]";
                }

                CovenVersion version = new();
                version.Tag = name;
                version.PublishedAt = publishedAt;

                foreach (JsonElement asset in assets.EnumerateArray()) {
                    string assetName = asset.GetRequiredString("name");
                    if (assetName.StartsWith("gex.Coven") == false) {
                        continue;
                    }

                    string assetUrl = asset.GetRequiredString("browser_download_url");

                    if (assetName.Contains("linux-x64", StringComparison.OrdinalIgnoreCase)) {
                        version.LinuxDownload = assetUrl;
                    } else if (assetName.Contains("win-x64", StringComparison.OrdinalIgnoreCase)) {
                        version.WindowsDownload = assetUrl;
                    } else {
                        return $"unchecked assetName (expected it to contain either 'linux' or 'windows') [name={assetName}]";
                    }
                }

                versions.Add(version);
            }

            if (versions.Count == 0) {
                return $"found no versions";
            }

            latestVersion = versions.OrderByDescending(iter => iter.PublishedAt).First();
            _Cache?.Set(CACHE_KEY_LATEST_VERSION, latestVersion, new MemoryCacheEntryOptions() {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2)
            });

            _Logger.LogInformation($"latest version loaded [version={latestVersion.Tag}]");

            return latestVersion;
        }

    }
}
