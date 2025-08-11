using gex.Models;
using gex.Models.Lobby;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Lobby {

    public interface ILobbyClient {

        Task<Result<bool, string>> Connect(CancellationToken cancel);

        Task<Result<bool, string>> Disconnect(CancellationToken cancel);

        Task<Result<LobbyMessage, string>> Login(CancellationToken cancel);

        Task<Result<bool, string>> Write(string message, CancellationToken cancel);

        Task<Result<LobbyMessage, string>> WriteReply(string message, TimeSpan timeout, CancellationToken cancel);

    }
}