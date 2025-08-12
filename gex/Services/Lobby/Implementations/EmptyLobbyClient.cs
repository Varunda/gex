using gex.Models;
using gex.Models.Lobby;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Lobby.Implementations {

    /// <summary>
    ///     an emtpy used when Spring is disabled in secrets.json
    /// </summary>
    public class EmptyLobbyClient : ILobbyClient {

        public Task<Result<bool, string>> Connect(CancellationToken cancel) {
            return Task.FromResult(Result<bool, string>.Err("Lobby not enabled"));
        }

        public Task<Result<bool, string>> Disconnect(CancellationToken cancel) {
            return Task.FromResult(Result<bool, string>.Err("Lobby not enabled"));
        }

        public Task<Result<LobbyMessage, string>> Login(CancellationToken cancel) {
            return Task.FromResult(Result<LobbyMessage, string>.Err("Lobby not enabled"));
        }

        public Task<Result<bool, string>> Write(string command, string message, CancellationToken cancel) {
            return Task.FromResult(Result<bool, string>.Err("Lobby not enabled"));
        }

        public Task<Result<LobbyMessage, string>> WriteReply(string command, string message, TimeSpan timeout, CancellationToken cancel) {
            return Task.FromResult(Result<LobbyMessage, string>.Err("Lobby not enabled"));
        }

        public bool IsConnected() {
            return false;
        }

        public DateTime LastMessage() {
            return DateTime.MinValue;
        }

    }
}
