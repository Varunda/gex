using gex.Models;
using gex.Models.Lobby;
using gex.Models.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Lobby {

    public interface ILobbyClient {

        /// <summary>
        ///     open the TCP connection to the lobby
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns></returns>
        Task<Result<bool, string>> Connect(CancellationToken cancel);

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
        Task<Result<LobbyMessage, string>> Login(CancellationToken cancel);

        /// <summary>
        ///     write a message to the lobby, and dont wait for a reply
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        Task<Result<bool, string>> Write(string command, string message, CancellationToken cancel);

        /// <summary>
        ///     write a message to the lobby, and wait for the reply
        /// </summary>
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
        ///     when was the last message received by the client
        /// </summary>
        /// <returns></returns>
        DateTime LastMessage();

    }
}