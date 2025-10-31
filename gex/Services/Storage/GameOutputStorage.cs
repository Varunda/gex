using gex.Common.Models;
using gex.Common.Models.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Storage {

    public class GameOutputStorage {

        private readonly ILogger<GameOutputStorage> _Logger;
        private readonly IOptions<FileStorageOptions> _Options;

        public GameOutputStorage(ILogger<GameOutputStorage> logger,
            IOptions<FileStorageOptions> options) {

            _Logger = logger;
            _Options = options;
        }

        /// <summary>
        ///		get the contents of an action log for a game
        /// </summary>
        /// <param name="gameID">ID of the game to get the action log for</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns>
        ///		a <see cref="Result{T, E}"/> that will contain the contents of the action log,
        ///		or an error if the action log could not be loaded
        /// </returns>
        public async Task<Result<string, string>> GetActionLog(string gameID, CancellationToken cancel) {
            string path = Path.Join(_Options.Value.GameLogLocation, gameID[..], gameID, "actions.json");

            if (File.Exists(path) == false) {
                return Result<string, string>.Err($"missing path '{path}'");
            }

            string contents = await File.ReadAllTextAsync(path, cancel);

            return Result<string, string>.Ok(contents);
        }

        /// <summary>
        ///		save the action log from a game output
        /// </summary>
        /// <param name="gameID">ID of the game to save the action log</param>
        /// <param name="contents">raw string contents of the action log</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns>a task for the async operation</returns>
        public async Task SaveActionLog(string gameID, string contents, CancellationToken cancel) {
            string path = Path.Join(_Options.Value.GameLogLocation, gameID[..2], gameID, "actions.json");

            if (File.Exists(path) == true) {
                _Logger.LogWarning($"action log already exists, not overwriting [gameID={gameID}] [path='{path}']");
                return;
            }

            await File.WriteAllTextAsync(path, gameID, cancel);
        }

        /// <summary>
        ///     get the stdout of a match in the game logs
        /// </summary>
        /// <param name="gameID">ID of the game to get stdout of</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        public async Task<Result<string, string>> GetStdout(string gameID, CancellationToken cancel) {
            string path = Path.Join(_Options.Value.GameLogLocation, gameID[..2], gameID, "stdout.txt");

            if (File.Exists(path) == false) {
                return Result<string, string>.Err($"missing path '{path}'");
            }

            string contents = await File.ReadAllTextAsync(path, cancel);

            return Result<string, string>.Ok(contents);
        }

    }
}
