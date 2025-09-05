using gex.Models;
using gex.Services.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Tests.Util {

    public class FsGithubDownloadRepository : IGithubDownloadRepository {

        private readonly ILogger<FsGithubDownloadRepository> _Logger;

        public FsGithubDownloadRepository(ILogger<FsGithubDownloadRepository> logger) {
            _Logger = logger;
        }

        public Task DownloadFolder(string folder, CancellationToken cancel) {
            throw new NotImplementedException();
        }

        public async Task<Result<string, string>> GetFile(string folder, string file, CancellationToken cancel) {
            string content = await File.ReadAllTextAsync($"./resources/github_data/{folder}/{file}", cancel);

            return Result<string, string>.Ok(content);
        }

        public Result<List<string>, string> GetFiles(string folder) {
            throw new NotImplementedException();
        }

        public bool HasFile(string folder, string file) {
            string path = Path.Join("./", "resources", "github_data", folder, file);
            _Logger.LogTrace($"called HasFile [path={path}] [absolute path={Path.GetFullPath(path)}");
            return File.Exists(path);
        }

    }
}
