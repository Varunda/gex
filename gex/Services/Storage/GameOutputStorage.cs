using gex.Common.Models;
using gex.Common.Models.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZstdSharp;

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
        ///     get the folder that contains all game logs
        /// </summary>
        /// <param name="gameID"></param>
        /// <returns></returns>
        public string GetGameLogLocation(string gameID) => Path.Join(_Options.Value.GameLogLocation, gameID[..2], gameID);

        /// <summary>
        ///     check if a game has actions.json or actions.zstd stored
        /// </summary>
        /// <param name="gameID">ID of the game</param>
        public bool HasActionLog(string gameID) {
            string folder = GetGameLogLocation(gameID);

            string jsonPath = Path.Join(folder, "actions.json");
            string zstdPath = Path.Join(folder, "actions.zstd");

            return File.Exists(jsonPath) || File.Exists(zstdPath);
        }
        
        /// <summary>
        ///     delete the actions.json and actions.zstd of a game
        /// </summary>
        /// <param name="gameID">ID of the game</param>
        public void DeleteActionLog(string gameID) {
            string folder = GetGameLogLocation(gameID);

            File.Delete(Path.Join(folder, "actions.json"));
            File.Delete(Path.Join(folder, "actions.zstd"));

            _Logger.LogTrace($"deleted action log for game [gameID={gameID}] [folder={folder}]");
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

            _Logger.LogDebug($"getting action log of game [gameID={gameID}]");

            string folder = GetGameLogLocation(gameID);

            string jsonPath = Path.Join(folder, "actions.json");
            if (File.Exists(jsonPath) == true) {
                _Logger.LogTrace($"action log exists as a json [gameID={gameID}] [path={jsonPath}]");
                string contents = await File.ReadAllTextAsync(jsonPath, cancel);
                return Result<string, string>.Ok(contents);
            }

            string zstdPath = Path.Join(folder, "actions.zstd");
            if (File.Exists(zstdPath) == true) {
                _Logger.LogTrace($"action log exists as a zstd [gameID={gameID}] [path={zstdPath}]");
                using FileStream zstdFile = File.OpenRead(zstdPath);
                using DecompressionStream ds = new(zstdFile);

                using MemoryStream ms = new();
                await ds.CopyToAsync(ms, cancel);

                ms.Position = 0;
                string contents = Encoding.UTF8.GetString(ms.ToArray());
                return Result<string, string>.Ok(contents);
            }

            return Result<string, string>.Err("failed to find action log (json or zstd) for game");
        }

        /// <summary>
        ///		save the action log from a game output
        /// </summary>
        /// <param name="gameID">ID of the game to save the action log</param>
        /// <param name="contents">raw string contents of the action log</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns>a task for the async operation completes</returns>
        public async Task SaveActionLog(string gameID, string contents, CancellationToken cancel) {
            string path = Path.Join(GetGameLogLocation(gameID), "actions.json");

            if (File.Exists(path) == true) {
                _Logger.LogWarning($"action log already exists, not overwriting [gameID={gameID}] [path='{path}']");
                return;
            }

            await File.WriteAllTextAsync(path, gameID, cancel);
            _Logger.LogDebug($"saved action log [gameID={gameID}] [size={contents.Length}] [path='{path}']");
        }

        /// <summary>
        ///     save the action log of a game to disk from an input FileStream
        /// </summary>
        /// <param name="gameID">ID of the game</param>
        /// <param name="input">FileStream containing the contents of the action log</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns>a task for when the async operation completes</returns>
        public async Task SaveActionLog(string gameID, FileStream input, CancellationToken cancel) {
            string path = Path.Join(GetGameLogLocation(gameID), "actions.json");

            if (File.Exists(path) == true) {
                _Logger.LogWarning($"action log already exists, not overwriting [gameID={gameID}] [path='{path}']");
                return;
            }

            using FileStream output = File.OpenWrite(path);
            await input.CopyToAsync(output, CancellationToken.None);

            _Logger.LogDebug($"saved action log [gameID={gameID}] [path='{path}']");
        }

        /// <summary>
        ///     save an action log of a game but compressed
        /// </summary>
        /// <param name="gameID"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task CompressActionLog(string gameID, CancellationToken cancel) {
            string path = Path.Join(GetGameLogLocation(gameID), "actions.zstd");

            if (File.Exists(path) == true) {
                _Logger.LogWarning($"compressed action log already exists, not overwriting [gameID={gameID}] [path='{path}']");
                return;
            }

            Result<string, string> contents = await GetActionLog(gameID, cancel);
            if (contents.IsOk == false) {
                _Logger.LogError($"failed to compress action log [gameID={gameID}] [error={contents.Error}]");
                return;
            }

            using FileStream output = File.OpenWrite(path);

            using Compressor comp = new(22);
            Span<byte> comped = comp.Wrap(Encoding.UTF8.GetBytes(contents.Value));

            output.Write(comped);

            _Logger.LogDebug($"saved compressed action log [gameID={gameID}] [size={comped.Length}] [path='{path}']");
        }

        /// <summary>
        ///     get the stdout of a match in the game logs
        /// </summary>
        /// <param name="gameID">ID of the game to get stdout of</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        public async Task<Result<string, string>> GetStdout(string gameID, CancellationToken cancel) {
            string path = Path.Join(GetGameLogLocation(gameID), "stdout.txt");

            if (File.Exists(path) == false) {
                return Result<string, string>.Err($"missing path '{path}'");
            }

            string contents = await File.ReadAllTextAsync(path, cancel);

            return Result<string, string>.Ok(contents);
        }

        /// <summary>
        ///     delete a saved stdout.txt for a match
        /// </summary>
        /// <param name="gameID">ID of the match</param>
        public void DeleteStdout(string gameID) {
            string folder = GetGameLogLocation(gameID);

            File.Delete(Path.Join(folder, "stdout.txt"));

            _Logger.LogTrace($"deleted stdout for game [gameID={gameID}]");
        }

        /// <summary>
        ///     delete a saved stderr.txt of a match
        /// </summary>
        /// <param name="gameID">ID of the match</param>
        public void DeleteStderr(string gameID) {
            string folder = GetGameLogLocation(gameID);

            File.Delete(Path.Join(folder, "stderr.txt"));

            _Logger.LogTrace($"deleted stderr for game [gameID={gameID}]");
        }

    }
}
