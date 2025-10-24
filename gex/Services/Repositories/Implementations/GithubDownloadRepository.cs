using gex.Code.ExtensionMethods;
using gex.Common.Models;
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

namespace gex.Services.Repositories.Implementations {

    public class GithubDownloadRepository : IGithubDownloadRepository {

        private readonly ILogger<GithubDownloadRepository> _Logger;
        private readonly IOptions<GitHubOptions> _Options;
        private readonly IOptions<FileStorageOptions> _FileOptions;

        private const string BASE_URL = "https://api.github.com/repos";

        private static readonly HttpClient _Http = new HttpClient();
        static GithubDownloadRepository() {
            _Http.DefaultRequestHeaders.UserAgent.ParseAdd("gex/0.1 (discord: varunda)");
            _Http.DefaultRequestHeaders.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
        }

        public GithubDownloadRepository(ILogger<GithubDownloadRepository> logger,
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
        /// <param name="folder">name of the folder within the GitHub repo that will contain the file</param>
        /// <param name="file">name of the file</param>
        /// <param name="cancel">cancellation token</param>
        public async Task<Result<string, string>> GetFile(string folder, string file, CancellationToken cancel) {
            string path = Path.Join(_FileOptions.Value.GitHubDataLocation, folder, file);
            if (File.Exists(path) == false) {
                return Result<string, string>.Err($"path does not exist [path={path}]");
            }

            string content = await File.ReadAllTextAsync(path, cancel);
            return Result<string, string>.Ok(content);
        }

        /// <summary>
        ///     get a list of all files within a folder
        /// </summary>
        /// <param name="folder">name of the folder within the GitHub repo to get the name of the files of</param>
        /// <returns></returns>
        public Result<List<string>, string> GetFiles(string folder) {
            string path = Path.Join(_FileOptions.Value.GitHubDataLocation, folder);
            if (Directory.Exists(path) == false) {
                return $"failed to find folder [path={path}]";
            }

            return Directory.GetFiles(path).Select(iter => Path.GetFileName(iter)).ToList();
        }

        public bool HasFile(string folder, string file) {
            string path = Path.Join(_FileOptions.Value.GitHubDataLocation, folder, file);
            return File.Exists(path);
        }

        public Task DownloadFolder(string folder, CancellationToken cancel) {
            return DownloadFolder(folder, false, cancel);

        }

        /// <summary>
        ///     download a folder from GitHub, placing it in the target folder named <paramref name="folder"/>
        ///     relative to the <see cref="FileStorageOptions.GitHubDataLocation"/> set in <see cref="FileStorageOptions"/>.
        ///     this method will not keep the directory stucture
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="force">will download be forced even if the latest commit is downloaded?</param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task DownloadFolder(string folder, bool force, CancellationToken cancel) {

            _Logger.LogDebug($"downloading folder from github [folder={folder}]");

            Result<GitHubRateLimits, string> limits = await GetRateLimits(cancel);
            if (limits.IsOk == false) {
                _Logger.LogError($"failed to get GitHub rate limits [error={limits.Error}]");
                return;
            }

            int requestsRemaining = limits.Value.Remaining;
            _Logger.LogInformation($"got GitHub rate limits [requests remaining={requestsRemaining}] [resets={limits.Value.Reset:u}]");

            string targetFolder = Path.Join(_FileOptions.Value.GitHubDataLocation, folder);
            if (File.Exists(targetFolder) == false) {
                Directory.CreateDirectory(targetFolder);
            }

            Result<GitHubCommit, string> latestCommit = await GetLatestCommit(folder, cancel);
            if (latestCommit.IsOk == false) {
                _Logger.LogWarning($"failed to get latest commit [error={latestCommit.Error}] [folder={folder}]");
            } else {
                string? downloadedCommit = await GetLatestCommitDownloaded(folder, cancel);
                if (downloadedCommit == null) {
                    _Logger.LogInformation($"no commit downloaded, need to get latest [folder={folder}]");
                } else {
                    _Logger.LogInformation($"comparing latest commit and downloaded commit [folder={folder}] [latest sha={latestCommit.Value.SHA}] [downloaded sha={downloadedCommit}]");
                    if (latestCommit.Value.SHA == downloadedCommit) {
                        if (force == false) {
                            _Logger.LogInformation($"folder is already updated [folder={folder}]");
                            return;
                        } else {
                            _Logger.LogInformation($"folder is alreayd updated, but a force was ran [folder={folder}] [sha={downloadedCommit}]");
                        }
                    }
                }
            }

            Queue<string> dirs = [];
            HashSet<string> visited = [];

            dirs.Enqueue(folder);
            visited.Add(folder);

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
                            if (entry.DownloadUrl != null) {
                                _Logger.LogDebug($"downloading file [name={entry.Name}]");
                                if (requestsRemaining-- == 0) {
                                    _Logger.LogWarning($"GitHub rate limit hit!");
                                }

                                await DownloadFile(folder, entry.Name, entry.DownloadUrl, cancel);
                            } else {
                                _Logger.LogWarning($"missing download file [name={entry.Name}]");
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
                await SaveLatestCommitDownloaded(folder, latestCommit.Value.SHA);
            }

            _Logger.LogDebug($"finished downloading folder [folder={folder}]");
        }

        /// <summary>
        ///     get a string that contains the full SHA of the latest commit downloaded,
        ///     or <c>null</c> if no commit was downloaded
        /// </summary>
        /// <param name="folder">path in the github repo to get the latest commit downloaded</param>
        /// <param name="cancel">cancellation token</param>
        private async Task<string?> GetLatestCommitDownloaded(string folder, CancellationToken cancel) {
            string target = Path.Join(_FileOptions.Value.GitHubDataLocation, folder, "latest_commit.txt");
            _Logger.LogDebug($"checking latest commit of downloaded user data [folder={folder}] [target={target}]");

            if (File.Exists(target) == false) {
                return null;
            }

            string contents = (await File.ReadAllTextAsync(target, cancel)).Trim();
            return contents;
        }

        /// <summary>
        ///     save a full SHA commit hash to the latest_commit.txt file.
        ///     intentionally lacks a CancellationToken
        /// </summary>
        /// <param name="folder">path within the GitHub repository</param>
        /// <param name="commit">commit to save</param>
        private async Task SaveLatestCommitDownloaded(string folder, string commit) {
            string target = Path.Join(_FileOptions.Value.GitHubDataLocation, folder, "latest_commit.txt");
            _Logger.LogDebug($"saving latest commit of downloaded user data [commit={commit}] [folder={folder}] [target={target}]");

            await File.WriteAllTextAsync(target, commit, CancellationToken.None);
        }

        /// <summary>
        ///     get the latest full SHA from the GitHub API to the path given
        /// </summary>
        /// <param name="path">path of the file/folder to get the latest commit of</param>
        /// <param name="cancel">cancellation token</param>
        private async Task<Result<GitHubCommit, string>> GetLatestCommit(string path, CancellationToken cancel) {
            string url = $"{BASE_URL}/{_Options.Value.UnitDataOrganization}/{_Options.Value.UnitDataRepository}/commits?path={path}&per_page=1";
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
        /// <param name="folder">name of the folder within the GitHub repo</param>
        /// <param name="fileName">name of the output file</param>
        /// <param name="downloadPath">full url to download the file from</param>
        /// <param name="cancel">cancellation token</param>
        private async Task<Result<bool, string>> DownloadFile(string folder, string fileName, string downloadPath, CancellationToken cancel) {
            HttpResponseMessage res = await _Http.GetAsync(downloadPath, cancel);
            if (res.IsSuccessStatusCode == false) {
                return $"got status code {res.StatusCode}";
            }

            string filePath = Path.Join(_FileOptions.Value.GitHubDataLocation, folder, fileName);
            using FileStream fs = File.Open(filePath, FileMode.Create);
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
