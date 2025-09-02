using gex.Code.ExtensionMethods;
using gex.Models;
using gex.Models.Options;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

    public class BarUnitGithubRepository {

        private readonly ILogger<BarUnitGithubRepository> _Logger;
        private readonly IOptions<GitHubOptions> _Options;
        private readonly IOptions<FileStorageOptions> _FileOptions;

        private const string BASE_URL = "https://api.github.com/repos";

        private static readonly HttpClient _Http = new HttpClient();
        static BarUnitGithubRepository() {
            _Http.DefaultRequestHeaders.UserAgent.ParseAdd("gex/0.1 (discord: varunda)");
            _Http.DefaultRequestHeaders.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
        }

        public BarUnitGithubRepository(ILogger<BarUnitGithubRepository> logger,
            IOptions<GitHubOptions> options, IOptions<FileStorageOptions> fileOptions) {

            _Logger = logger;
            _Options = options;
            _FileOptions = fileOptions;

            if (_Options.Value.ApiKey != null) {
                _Http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {_Options.Value.ApiKey}");
                _Logger.LogDebug($"using github api token");
            }
        }

        /// <summary>
        ///     get the contents of the .lua file that contains the unit data
        /// </summary>
        /// <param name="definitionName">definition name of the unit</param>
        /// <param name="cancel">cancellation token</param>
        public async Task<Result<string, string>> GetUnitData(string definitionName, CancellationToken cancel) {
            string path = Path.Join(_FileOptions.Value.UnitDataLocation, $"{definitionName}.lua");
            if (File.Exists(path) == false) {
                return Result<string, string>.Err($"path does not exist [path={path}]");
            }

            string content = await File.ReadAllTextAsync(path, cancel);
            return Result<string, string>.Ok(content);
        }

        /// <summary>
        ///     download all updated unit data if needed
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task DownloadAll(CancellationToken cancel) {
            Result<GitHubRateLimits, string> limits = await GetRateLimits(cancel);
            if (limits.IsOk == false) {
                _Logger.LogError($"failed to get GitHub rate limits [error={limits.Error}]");
                return;
            }

            int requestsRemaining = limits.Value.Remaining;
            _Logger.LogInformation($"got GitHub rate limits [requests remaining={requestsRemaining}] [resets={limits.Value.Reset:u}]");

            Result<GitHubCommit, string> latestCommit = await GetLatestCommit(cancel);
            if (latestCommit.IsOk == false) {
                _Logger.LogWarning($"failed to get latest commit [error={latestCommit.Error}]");
            } else {
                string? downloadedCommit = await GetLatestCommitDownloaded(cancel);
                if (downloadedCommit == null) {
                    _Logger.LogInformation($"no commit downloaded, need to get latest");
                } else {
                    _Logger.LogInformation($"comparing latest commit and downloaded commit [latest sha={latestCommit.Value.SHA}] [downloaded sha={downloadedCommit}]");
                    if (latestCommit.Value.SHA == downloadedCommit) {
                        _Logger.LogInformation($"no unit data update needed");
                        return;
                    }
                }
            }

            Queue<string> dirs = [];
            HashSet<string> visited = [];

            dirs.Enqueue(_Options.Value.UnitDataPath);
            visited.Add(_Options.Value.UnitDataPath);

            string? dir = dirs.Dequeue();
            while (dir != null) {
                _Logger.LogDebug($"getting dir [dir={dir}]");

                cancel.ThrowIfCancellationRequested();

                if (requestsRemaining-- == 0) {
                    _Logger.LogWarning($"GitHub rate limit hit!");
                }

                Result<List<GitHubContentsEntry>, string> dirData = await GetDirectory(dir, cancel);
                if (dirData.IsOk == false) {
                    _Logger.LogError($"failed to get GitHub dir contents [dir={dir}] [error={dirData.Error}]");
                } else {
                    foreach (GitHubContentsEntry entry in dirData.Value) {
                        cancel.ThrowIfCancellationRequested();

                        if (entry.Type == "dir") {
                            _Logger.LogDebug($"queueing dir for download [path={entry.Path}]");

                            if (visited.Contains(entry.Path) == false) {
                                dirs.Enqueue(entry.Path);
                                visited.Add(entry.Path);
                            }
                        } else if (entry.Type == "file") {
                            if (entry.Name.EndsWith(".lua")) {
                                if (entry.DownloadUrl != null) {
                                    _Logger.LogDebug($"downloading file [name={entry.Name}]");
                                    if (requestsRemaining-- == 0) {
                                        _Logger.LogWarning($"GitHub rate limit hit!");
                                    }

                                    await DownloadUnitData(entry.Name, entry.DownloadUrl, cancel);
                                } else {
                                    _Logger.LogWarning($"missing download file [name={entry.Name}]");
                                }
                            } else {
                                _Logger.LogDebug($"skipping non-lua file to download [name={entry.Name}]");
                            }
                        }
                    }
                }

                if (dirs.Count == 0) {
                    break;
                }

                dir = dirs.Dequeue();
            }

            // in the case where an update takes place while Gex is downloading all of this data,
            // it is better for Gex to save the commit at the start, as the next time this method is called
            // Gex would see the outdated commit (even if SOME of the files are updated with the new commit)
            if (latestCommit.IsOk == true) {
                await SaveLatestCommitDownloaded(latestCommit.Value.SHA);
            }

            _Logger.LogDebug($"done!");
        }

        /// <summary>
        ///     get a string that contains the full SHA of the latest commit downloaded,
        ///     or <c>null</c> if no commit was downloaded
        /// </summary>
        /// <param name="cancel">cancellation token</param>
        private async Task<string?> GetLatestCommitDownloaded(CancellationToken cancel) {
            string path = Path.Join(_FileOptions.Value.UnitDataLocation, "latest_commit.txt");
            _Logger.LogDebug($"checking latest commit of downloaded user data [path={path}]");

            if (File.Exists(path) == false) {
                return null;
            }

            string contents = (await File.ReadAllTextAsync(path, cancel)).Trim();
            return contents;
        }

        /// <summary>
        ///     save a full SHA commit hash to the latest_commit.txt file.
        ///     intentionally lacks a CancellationToken
        /// </summary>
        /// <param name="commit">commit to save</param>
        private async Task SaveLatestCommitDownloaded(string commit) {
            string path = Path.Join(_FileOptions.Value.UnitDataLocation, "latest_commit.txt");
            _Logger.LogDebug($"saving latest commit of downloaded user data [commit={commit}] [path={path}]");

            await File.WriteAllTextAsync(path, commit, CancellationToken.None);
        }

        /// <summary>
        ///     get the latest full SHA from the GitHub API to the folder that has the unit data
        /// </summary>
        /// <param name="cancel">cancellation token</param>
        private async Task<Result<GitHubCommit, string>> GetLatestCommit(CancellationToken cancel) {
            string url = $"{BASE_URL}/{_Options.Value.UnitDataOrganization}/{_Options.Value.UnitDataRepository}/commits?path={_Options.Value.UnitDataPath}&per_page=1";
            _Logger.LogDebug($"getting latest commit from GitHub api [url={url}]");
            Result<JsonElement, string> res = await _Http.GetJsonAsync(url, cancel);
            if (res.IsOk == false) {
                return res.Error;
            }

            JsonElement json = res.Value;

            if (json.ValueKind != JsonValueKind.Array) {
                return $"expected an array, was a {json.ValueKind} instead";
            }

            foreach (JsonElement iter in json.EnumerateArray()) {
                string? sha = iter.GetRequiredChild("sha").GetString();
                if (sha == null) {
                    return "missing sha field from json";
                }

                return new GitHubCommit() { SHA = sha };
            }

            return "no elements in array";
        }

        /// <summary>
        ///     get the contents of a directory from the GitHub API
        /// </summary>
        /// <param name="dir">directory within the repository to get the contents of</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        private async Task<Result<List<GitHubContentsEntry>, string>> GetDirectory(string dir, CancellationToken cancel) {
            string url = $"{BASE_URL}/{_Options.Value.UnitDataOrganization}/{_Options.Value.UnitDataRepository}/contents/{dir}";
            _Logger.LogDebug($"getting url [url={url}]");
            HttpResponseMessage res = await _Http.GetAsync(url, cancel);
            _Logger.LogTrace($"got url [url={url}] [statusCode={res.StatusCode}]");
            if (res.IsSuccessStatusCode == false) {
                return $"got status code {res.StatusCode}";
            }

            byte[] body = await res.Content.ReadAsByteArrayAsync(cancel);

            JsonElement json = JsonSerializer.Deserialize<JsonElement>(body);
            if (json.ValueKind != JsonValueKind.Array) {
                return $"expected array from serialized JSON, got {json.ValueKind} instead";
            }

            List<GitHubContentsEntry> entries = [];
            foreach (JsonElement elem in json.EnumerateArray()) {
                entries.Add(GitHubContentsEntry.FromJson(elem));
            }

            return entries;
        }

        /// <summary>
        ///     download a specific file
        /// </summary>
        /// <param name="fileName">name of the output file</param>
        /// <param name="downloadPath">full url to download the file from</param>
        /// <param name="cancel">cancellation token</param>
        private async Task<Result<bool, string>> DownloadUnitData(string fileName, string downloadPath, CancellationToken cancel) {
            HttpResponseMessage res = await _Http.GetAsync(downloadPath, cancel);
            if (res.IsSuccessStatusCode == false) {
                return $"got status code {res.StatusCode}";
            }

            string filePath = Path.Join(_FileOptions.Value.UnitDataLocation, fileName);
            using FileStream fs = File.OpenWrite(filePath);
            await res.Content.CopyToAsync(fs, cancel);

            return true;
        }

        /// <summary>
        ///     get the rate limits from the GitHub API
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns></returns>
        private async Task<Result<GitHubRateLimits, string>> GetRateLimits(CancellationToken cancel) {
            HttpResponseMessage res = await _Http.GetAsync("https://api.github.com/rate_limit", cancel);
            if (res.IsSuccessStatusCode == false) {
                return $"got status code {res.StatusCode}";
            }

            byte[] body = await res.Content.ReadAsByteArrayAsync(cancel);
            JsonElement json = JsonSerializer.Deserialize<JsonElement>(body);

            JsonElement? rate = json.GetChild("rate");
            if (rate == null) {
                return $"missing element 'rate' from {json}";
            }

            GitHubRateLimits limits = new();
            limits.Service = "rate";

            limits.Limit = rate.Value.GetRequiredChild("limit").GetInt32();
            limits.Used = rate.Value.GetRequiredChild("used").GetInt32();
            limits.Remaining = rate.Value.GetRequiredChild("remaining").GetInt32();
            limits.Reset = DateTimeOffset.FromUnixTimeSeconds(rate.Value.GetRequiredChild("reset").GetInt32()).DateTime;

            return limits;
        }

        private class GitHubContentsEntry {

            public string Name { get; set; } = "";

            public string Path { get; set; } = "";

            public string? DownloadUrl { get; set; }

            public string Type { get; set; } = "";

            public static GitHubContentsEntry FromJson(JsonElement elem) {
                GitHubContentsEntry entry = new();

                entry.Name = elem.GetChild("name")?.GetString() ?? throw new Exception($"missing 'Name' from {elem}");
                entry.Path = elem.GetChild("path")?.GetString() ?? throw new Exception($"missing 'Path' from {elem}");
                entry.Type = elem.GetChild("type")?.GetString() ?? throw new Exception($"missing 'Type' from {elem}");
                entry.DownloadUrl = elem.GetChild("download_url")?.GetString(); // can be null if |Type| is dir

                return entry;
            }

        }

        private class GitHubRateLimits {

            public string Service { get; set; } = "";

            public int Limit { get; set; }

            public int Used { get; set; }

            public int Remaining { get; set; }

            public DateTime Reset { get; set; }

        }

        public class GitHubCommit {

            public string SHA { get; set; } = "";


        }

    }
}
