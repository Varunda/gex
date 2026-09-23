using gex.Common.Models;
using gex.Coven.Models.Config;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven.Services.Util {

    public class StorageUtil {

        private readonly ILogger<StorageUtil> _Logger;
        private readonly UserOptionsService _UserOptions;

        public StorageUtil(ILogger<StorageUtil> logger,
            UserOptionsService userOptions) {

            _Logger = logger;
            _UserOptions = userOptions;
        }

        public bool HasActionLog(string gameID) {
            UserOptions userOptions = _UserOptions.Load();
            string path = Path.Join(ShellUtil.GetWorkingDirectory(), "actions", $"action-{gameID}.json");
            return File.Exists(path);
        }

        public async Task SaveActionLog(string gameID, string contents, CancellationToken cancel) {
            UserOptions userOptions = _UserOptions.Load();
            string path = Path.Join(ShellUtil.GetWorkingDirectory(), "actions", $"action-{gameID}.json");

            Directory.CreateDirectory(Path.Join(ShellUtil.GetWorkingDirectory(), "actions"));

            await File.WriteAllTextAsync(path, contents, cancel);
        }

        public async Task<Result<string, string>> GetActionLog(string gameID, CancellationToken cancel) {
            UserOptions userOptions = _UserOptions.Load();
            string path = Path.Join(ShellUtil.GetWorkingDirectory(), "actions", $"action-{gameID}.json");

            if (HasActionLog(gameID) == false) {
                return Result<string, string>.Err($"missing action log file [path={path}]");
            }

            string contents = await File.ReadAllTextAsync(path, cancel);
            return Result<string, string>.Ok(contents);
        }

    }
}
