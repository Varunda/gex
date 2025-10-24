using gex.Common.Models;
using gex.Common.Models.Lobby;
using System;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Lobby {

    public interface ILobbyClient {

        /// <summary>
        ///     open the TCP connection to the lobby
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns></returns>
        Task<Result<bool, string>> Connect(string host, int port, CancellationToken cancel);

        /// <summary>
        ///     send the EXIT command to the lobby and disconnect the TCP socket
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns></returns>
        Task<Result<bool, string>> Exit(CancellationToken cancel);

        /// <summary>
        ///     disconnect from the TCP socket to the lobby
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns></returns>
        Task<Result<bool, string>> Disconnect(CancellationToken cancel);

        /// <summary>
        ///     perform a login using the credentials in <see cref="SpringLobbyOptions"/>
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns></returns>
        Task<Result<LobbyMessage, string>> Login(string username, string password, CancellationToken cancel);

        /// <summary>
        ///     write a message to the lobby, and dont wait for a reply
        /// </summary>
        /// <param name="command">command being sent</param>
        /// <param name="message"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        Task<Result<bool, string>> Write(string command, string message, CancellationToken cancel);

        /// <summary>
        ///     write a message to the lobby, and wait for the reply
        /// </summary>
        /// <param name="command">command being sent</param>
        /// <param name="message">message to send. do not include the trailing \n</param>
        /// <param name="timeout">timeout before the reply is considered lost</param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        Task<Result<LobbyMessage, string>> WriteReply(string command, string message, TimeSpan timeout, CancellationToken cancel);

        /// <summary>
        ///     is the client currently connected
        /// </summary>
        /// <returns></returns>
        bool IsConnected();

        /// <summary>
        ///     is the client currently performing a login
        /// </summary>
        /// <returns></returns>
        bool IsLoggingIn();

        /// <summary>
        ///     is the client both connected and logged in
        /// </summary>
        /// <returns></returns>
        bool IsLoggedIn();

        /// <summary>
        ///     when was the last message received by the client
        /// </summary>
        /// <returns></returns>
        DateTime LastMessage();

        event EventHandler<LobbyMessage>? OnMessageReceived;
        void EmitMessageReceived(LobbyMessage message);

        Task<Result<LobbyWhoisResponse, string>> Whois(string username, CancellationToken cancel);

        Task<Result<LobbyBattleStatus, string>> BattleStatus(int battleID, CancellationToken cancel);

    }
}