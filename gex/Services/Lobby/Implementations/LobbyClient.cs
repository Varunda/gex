using gex.Models;
using gex.Models.Lobby;
using gex.Models.Options;
using gex.Services.Metrics;
using gex.Services.Queues;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Lobby.Implementations {

    public class LobbyClient : ILobbyClient {

        private readonly ILogger<LobbyClient> _Logger;
        private readonly IOptions<SpringLobbyOptions> _Options;
        private readonly LobbyClientMetric _Metric;
        private readonly BaseQueue<LobbyMessage> _LobbyMessageQueue;

        /// <summary>
        ///     TPC socket that connects to the lobby
        /// </summary>
        private TcpClient _TcpSocket;

        /// <summary>
        ///     incrementing ID for writing and expecting a reply
        /// </summary>
        private static int _MessageId = 1;

        /// <summary>
        ///     messages with a message ID that are pending the reply to be sent to the writer
        /// </summary>
        private readonly Dictionary<int, LobbyMessage> _PendingMessages = [];

        /// <summary>
        ///     hash set of message IDs gex expects to see a response for. if a command is received with an unexpected message ID,
        ///     then it is sent to the processing queue
        /// </summary>
        private readonly HashSet<int> _ExpectedMessages = [];

        /// <summary>
        ///     when the last byte was received from the lobby client
        /// </summary>
        private DateTime _LastMessageReceived = DateTime.MinValue;

        /// <summary>
        ///     indicates if the client is connected or not
        /// </summary>
        private bool _LoggedIn = false;

        private CancellationTokenSource _Cancel = new();
        private Task? _ReadTask;
        private Task? _PingTask;

        /// <summary>
        ///     disconnecting is done with a semaphore to prevent the socket from getting into a bad state
        /// </summary>
        private readonly SemaphoreSlim _Signal = new(1, 1);

        /// <summary>
        ///     signal to ensure only 1 $whois command is being processed at a time
        /// </summary>
        private readonly SemaphoreSlim _WhoisSignal = new(1, 1);

        /// <summary>
        ///     signal to indicate a $whois response is ready to be read
        /// </summary>
        private readonly SemaphoreSlim _WhoisReady = new(0, 1);

        /// <summary>
        ///     is the client expecting to see multiple messages that indicate a DM From "Coordinator" that
        ///     contains the $whois response
        /// </summary>
        private bool _ExpectedWhoisResponse = false;

        /// <summary>
        ///     list of messages that are building a $whois response
        /// </summary>
        private List<LobbyMessage> _PendingWhoisResponse = [];

        public LobbyClient(ILogger<LobbyClient> logger,
            IOptions<SpringLobbyOptions> options, BaseQueue<LobbyMessage> lobbyMessageQueue,
            LobbyClientMetric metric) {

            _TcpSocket = new TcpClient();

            _Logger = logger;
            _Options = options;
            _Metric = metric;
            _LobbyMessageQueue = lobbyMessageQueue;

            if (string.IsNullOrEmpty(_Options.Value.Host)) {
                throw new Exception($"missing Spring:Host. set this in secrets.json, or disable Spring by settings Spring:Enabled to false");
            }
            if (_Options.Value.Port == 0) {
                throw new Exception($"missing Spring:Port. set this in secrets.json, or disable Spring by settings Spring:Enabled to false");
            }
            if (string.IsNullOrEmpty(_Options.Value.Username)) {
                throw new Exception($"missing Spring:Username. set this in secrets.json, or disable Spring by settings Spring:Enabled to false");
            }
            if (string.IsNullOrEmpty(_Options.Value.Password)) {
                throw new Exception($"missing Spring:Password. set this in secrets.json, or disable Spring by settings Spring:Enabled to false");
            }
        }

        private string _LobbyHost => _Options.Value.Host;
        private int _LobbyPort => _Options.Value.Port;

        public bool IsConnected() {
            _Signal.Wait();
            bool connected = _TcpSocket.Connected;
            _Signal.Release();
            return connected;
        }

        public bool IsLoggedIn() {
            return IsConnected() && _LoggedIn;
        }

        public DateTime LastMessage() => _LastMessageReceived;

        public async Task<Result<bool, string>> Connect(CancellationToken cancel) {
            if (_TcpSocket.Connected == true) {
                _Logger.LogInformation($"lobby client is already connected, reconnecting");
                Result<bool, string> dis = await Disconnect(cancel);
                if (dis.IsOk == false) {
                    return dis;
                }

                if (_TcpSocket.Connected == true) {
                    _Logger.LogWarning($"call to disconnect did not actually disconnect the TCP socket");
                    return "failed to disconnect (client was already connected)";
                }
            }

            try {
                _Logger.LogInformation($"lobby client connecting to host [host={_LobbyHost}:{_LobbyPort}]");
                await _TcpSocket.ConnectAsync(_LobbyHost, _LobbyPort, cancel);
                _Logger.LogDebug($"lobby client TCP connection made [connected={_TcpSocket.Connected}]");

                _ReadTask = Task.Run(() => {
                    _Logger.LogDebug($"started read task");
                    try {
                        ReadThread(_TcpSocket, _Cancel.Token);
                    } catch (Exception) when (_Cancel.Token.IsCancellationRequested == true) {
                        _Logger.LogInformation($"read task cancelled");
                    } catch (Exception ex) when (_Cancel.Token.IsCancellationRequested == false) {
                        _Logger.LogError(ex, $"error in readThread");
                    }
                    _Logger.LogDebug($"read task done");
                }, _Cancel.Token);

                _PingTask = Task.Run(() => {
                    _Logger.LogDebug($"started ping task");
                    try {
                        PingWriteThread(_Cancel.Token);
                    } catch (Exception) when (_Cancel.Token.IsCancellationRequested == true) {
                        _Logger.LogInformation($"closing ping write thread safely due to cancellation token");
                    } catch (Exception ex) when (_Cancel.Token.IsCancellationRequested == false) {
                        _Logger.LogError(ex, "error in ping write thread");
                    }
                    _Logger.LogDebug($"ping task done");
                }, _Cancel.Token);
            } catch (Exception ex) {
                _Logger.LogError(ex, "failed to connect to host");
                return ex.Message;
            }

            return true;
        }

        public async Task<Result<bool, string>> Exit(CancellationToken cancel) {
            _LoggedIn = false;

            if (_TcpSocket.Connected == false) {
                return true;
            }

            await Write("EXIT", "", cancel);

            return await Disconnect(cancel);
        }

        public async Task<Result<bool, string>> Disconnect(CancellationToken cancel) {
            _LoggedIn = false;

            if (_TcpSocket.Connected == false) {
                return true;
            }

            _Signal.Wait(cancel);
            try {
                _Cancel.Cancel();
                if (_ReadTask != null) {
                    await _ReadTask.WaitAsync(cancel);
                }
                if (_PingTask != null) {
                    await _PingTask.WaitAsync(cancel);
                }
                _Cancel = new CancellationTokenSource(); // create a new token

                _TcpSocket.Close();
                _TcpSocket = new TcpClient();
                _Logger.LogDebug($"disconnect done");
                return true;
            } catch (Exception ex) {
                _Logger.LogError(ex, $"error disconnecting");
                return false;
            } finally {
                _Signal.Release();
            }
        }

        /// <summary>
        ///     perform a login
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<Result<LobbyMessage, string>> Login(CancellationToken cancel) {
            if (_TcpSocket.Connected == false) {
                return "TCP socket is not connected";
            }

            _Logger.LogDebug($"logging in to Spring lobby [host={_LobbyHost}:{_LobbyPort}] [user={_Options.Value.Username}]");

            byte[] md5 = MD5.HashData(Encoding.UTF8.GetBytes(_Options.Value.Password));
            string password = Convert.ToBase64String(md5);

            Result<LobbyMessage, string> loginRequest = await WriteReply(
                "LOGIN", $"{_Options.Value.Username} {password} 0 * LuaLobby Chobby\t:3 :3\tb sp", TimeSpan.FromSeconds(5), cancel
            );

            return loginRequest;
        }

        public Task<Result<bool, string>> Write(string command, string message, CancellationToken cancel) {
            return Write(null, command, message, cancel);
        }

        /// <summary>
        ///     write a command, dont include the trailing \n
        /// </summary>
        /// <param name="msgId"></param>
        /// <param name="command"></param>
        /// <param name="message"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<Result<bool, string>> Write(int? msgId, string command, string message, CancellationToken cancel) {
            if (_TcpSocket.Connected == false) {
                _Logger.LogWarning($"cannot write command, socket is not connected [message={message}]");
                return "not connected";
            }

            string cmd = $"{(msgId != null ? $"#{msgId} ": "")}{command}{(message != "" ? $" {message}" : "")}\n";
            //_Logger.LogTrace($"SEND>> {cmd}");
            byte[] msg = Encoding.UTF8.GetBytes(cmd);
            try {
                await _TcpSocket.GetStream().WriteAsync(msg, cancel);
            } catch (Exception ex) {
                _Metric.RecordWriteError(command);
                _Logger.LogError(ex, $"failed to write message [message={message}]");

                if (ex.Message == "Unable to write data to the transport connection: An established connection was aborted by the software in your host machine.") {
                    _TcpSocket.Close();
                }

                return ex.Message;
            }

            _Metric.RecordCommandSent(command);

            return true;
        }

        /// <summary>
        ///     write a command with a message ID, and wait for a reply with that message ID.
        ///     do not include the trailing \n
        /// </summary>
        /// <param name="command">command</param>
        /// <param name="message"></param>
        /// <param name="timeout"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<Result<LobbyMessage, string>> WriteReply(string command, string message, TimeSpan timeout, CancellationToken cancel) {
            int msgId = _MessageId++;
            Result<bool, string> write = await Write(msgId, command, message, cancel);
            if (write.IsOk == false) {
                return write.Error;
            }

            DateTime timeoutEnd = DateTime.UtcNow + timeout;
            _ExpectedMessages.Add(msgId);

            LobbyMessage? response = null;
            while (response == null) {
                response = _PendingMessages.GetValueOrDefault(msgId);

                if (response != null) {
                    _ExpectedMessages.Remove(msgId);
                    break;
                }

                if (timeoutEnd <= DateTime.UtcNow) {
                    _Logger.LogWarning($"got a timeout when waiting for a response for command [messageID={msgId}]");
                    break;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(100), cancel);
            }

            if (response == null) {
                return "failed to get response after timeout";
            }

            return response;
        }

        public async Task<Result<LobbyWhoisResponse, string>> Whois(string username, CancellationToken cancel) {
            _Logger.LogTrace($"starting whois [username={username}]");
            await _WhoisSignal.WaitAsync(cancel);
            _Logger.LogTrace($"entered whois [username={username}]");

            _ExpectedWhoisResponse = true;

            try {
                Result<bool, string> response = await Write("SAYPRIVATE", $"Coordinator $whois {username}", cancel);

                await _WhoisReady.WaitAsync(cancel);
                if (_PendingWhoisResponse.Count == 0) {
                    _Logger.LogInformation($"failed to find whois user [username={username}]");
                    return $"Coordinator did not have user [username={username}]";
                }

                List<LobbyMessage> responses = new(_PendingWhoisResponse);

                _Logger.LogTrace($"got responses for $whois [username='{username}'] [count={responses.Count}] [pending count={_PendingWhoisResponse.Count}]");

                // 
                // example response
                // 
                // Found BobAlice
                // Previous names: AliceBob
                // Profile link: https://server4.beyondallreason.info/profile/1234
                // Chevron level: 3
                // Ratings:
                // Duel > Game: 12.34, Leaderboard: 15.78
                // FFA > Game: 14.2, Leaderboard: 0.0
                // Large Team > Game: 28.25, Leaderboard: 13.9
                // Small Team > Game: 12.35, Leaderboard: 3.15
                // No moderation restrictions applied.
                // 
                LobbyWhoisResponse whois = new();
                for (int i = 0; i < responses.Count; ++i) {
                    LobbyMessage msg = responses[i];

                    string? msgUsername = msg.GetWord();
                    string? msgContent = msg.GetSentence();

                    if (msgUsername == null || msgContent == null) {
                        _Logger.LogWarning($"missing msgUsername or msgContent [msgUsername={msgUsername}] [msgContent={msgContent}] [username={username}]");
                        continue;
                    }

                    if (i == 0) {
                        if (msgContent.StartsWith("Found")) {
                            whois.Username = msgContent.Replace("Found", "").Trim();
                        }
                    } else if (i == 1) {
                        if (msgContent.StartsWith("Previous name: ")) {
                            whois.PreviousNames = [..msgContent["Previous names: ".Length..].Split(", ")];
                        }
                    } else if (i == 2) {
                        if (msgContent.StartsWith("Profile link: ")) {
                            Regex reg = new(@"Profile link: https+://.*?/profile/(\d+)");
                            Match match = reg.Match(msgContent);
                            if (match.Success == false) {
                                _Logger.LogWarning($"failed to match profile link regex [msgContent={msgContent}] [username={username}]");
                            } else {
                                string userID = match.Groups[1].Value;
                                if (long.TryParse(userID, out long uID) == false) {
                                    _Logger.LogWarning($"failed to parse profile ID to a valid int64 [userID={userID}] [username={username}]");
                                } else {
                                    whois.UserID = uID;
                                }
                            }
                        }
                    } else if (i == 3) {
                        if (msgContent.StartsWith("Chevron level: ")) {
                            string chevron = msgContent["Chevron level: ".Length..].Trim();
                            if (int.TryParse(chevron, out int chev) == false) {
                                _Logger.LogWarning($"failed to parse chevron for user [chevron={chevron}] [username={username}]");
                            } else {
                                whois.Chevron = chev;
                            }
                        }
                    } else if (i == 4) {
                        if (msgContent == "Ratings:") {

                        }
                    } else if (msgContent.StartsWith("No moderation")) {
                        continue;
                    } else if (i > 4) {
                        string[] parts = msgContent.Split(">");
                        if (parts.Length != 2) {
                            _Logger.LogWarning($"unexpected number of parts, wanted 2 [parts.Length={parts.Length}] [username={username}]");
                        } else {
                            string gamemode = parts[0].Trim();
                            string ratings = parts[1].Trim();

                            Regex reg = new(@"Game: (\d{1,2}\.\d{1,2}), Leaderboard: (\d{1,2}\.\d{1,2})");
                            Match match = reg.Match(ratings);
                            if (match.Success == false) {
                                _Logger.LogWarning($"failed to match ratings to regex for parsing [ratings='{ratings}'] [username={username}]");
                            } else {
                                WhoisRating rating = new();
                                rating.Gamemode = gamemode;
                                if (float.TryParse(match.Groups[1].Value, out float gameRating) == false) {
                                    _Logger.LogWarning($"failed to convert game rating into a valid float [value={match.Groups[0].Value}] [username={username}]");
                                } else {
                                    rating.Rating = gameRating;
                                }

                                if (float.TryParse(match.Groups[2].Value, out float lbRating) == false) {
                                    _Logger.LogWarning($"failed to convert leaderboard rating into a valid float [value={match.Groups[1].Value}] [username={username}]");
                                } else {
                                    rating.Leaderboard = lbRating;
                                }

                                whois.Ratings[gamemode] = rating;
                            }
                        }
                    }
                }

                return whois;
            } catch (Exception ex) {
                _Logger.LogError(ex, $"failed to perform whois [username={username}]");
                return ex.Message;
            } finally {
                _PendingWhoisResponse.Clear();
                _ExpectedWhoisResponse = false;
                _WhoisSignal.Release();
            }
        }

        /// <summary>
        ///     reader method that handles reading from the TCP socket
        /// </summary>
        /// <param name="client"></param>
        /// <param name="cancel"></param>
        private void ReadThread(TcpClient client, CancellationToken cancel) {
            _Logger.LogInformation($"client read thread started");
            NetworkStream stream = client.GetStream();

            byte[] buffer = new byte[1024];
            List<byte> message = new();

            while (cancel.IsCancellationRequested == false) {
                while (!stream.DataAvailable) {
                    cancel.ThrowIfCancellationRequested();

                    if (client.Connected == false) {
                        break;
                    }
                }

                if (client.Connected == false) {
                    _Logger.LogInformation($"client disconnected");
                    break;
                }

                _LastMessageReceived = DateTime.UtcNow;

                int amt = stream.Read(buffer, 0, 1024);
                message.AddRange(buffer.AsSpan()[..amt]);

                //_Logger.LogTrace($"tcp read [amt={amt}] [str={string.Join("", message.Select(iter => (char)iter))}]");

                while (cancel.IsCancellationRequested == false) {
                    cancel.ThrowIfCancellationRequested();

                    LobbyMessage? msg = parseMessage(message);
                    if (msg == null) {
                        break;
                    }

                    _Metric.RecordCommandReceived(msg.Command);

                    //_Logger.LogDebug($"RECV || {msg.Command}: {msg.Arguments}");

                    if (_ExpectedWhoisResponse == true && msg.Command == "SAIDPRIVATE" && msg.Arguments.StartsWith("Coordinator")) {
                        _Logger.LogTrace($"got part of message DM [message={msg.Arguments}]");

                        // Coordinator Unable to find a user with that name
                        // Coordinator Unable to find a user with that name
                        if (msg.Arguments == "Coordinator Unable to find a user with that name") {
                            _Logger.LogDebug($"finished getting $whois response");
                            _WhoisReady.Release();
                        } else if (msg.Arguments == "Coordinator ---------------------------") {
                            if (_PendingWhoisResponse.Count > 0) {
                                _Logger.LogDebug($"finished getting $whois response");
                                _WhoisReady.Release();
                            }
                        } else {
                            _PendingWhoisResponse.Add(msg);
                        }
                    } else if (msg.MessageId != null) {
                        if (_ExpectedMessages.Contains(msg.MessageId.Value) == false) {
                            _Logger.LogWarning($"unexpected message ID, queuing up [messageID={msg.MessageId}] [command={msg.Command}]");
                            _LobbyMessageQueue.Queue(msg);
                        } else {
                            _ExpectedMessages.Remove(msg.MessageId.Value);
                            if (_PendingMessages.ContainsKey(msg.MessageId.Value) == true) {
                                _Logger.LogWarning($"colliding message ID response from server found [messageID={msg.MessageId}] [command={msg.Command}]");
                            } else {
                                _PendingMessages.Add(msg.MessageId.Value, msg);
                                _Logger.LogTrace($"added message id response [messageID={msg.MessageId}] [command={msg.Command}]");
                            }
                        }
                    } else if (msg.Command == "LOGININFOEND") {
                        _LoggedIn = true;
                    } else {
                        _LobbyMessageQueue.Queue(msg);
                    }

                    if (message.Count == 0) {
                        break;
                    }
                }
            }
        }

        /// <summary>
        ///     ping writer method that pings the spring lobby every 30 seconds so it knows Gex is still connected
        /// </summary>
        /// <param name="cancel"></param>
        private async void PingWriteThread(CancellationToken cancel) {
            _Logger.LogInformation($"ping write started");

            while (cancel.IsCancellationRequested == false) {
                // delay at the start so PING is sent 30 seconds after login
                // 2025-08-11 FIXME: why is a try/catch needed here? if cancelled, visual studio throws an exception from here,
                //      instead of it being caught in the Task that runs this method
                try {
                    await Task.Delay(TimeSpan.FromSeconds(30), cancel);
                } catch (TaskCanceledException) {
                    //_Logger.LogDebug($"delay in ping thread canceled");
                    break;
                }

                Result<LobbyMessage, string> pong = await WriteReply("PING", "", TimeSpan.FromSeconds(5), cancel);
                if (pong.IsOk == false) {
                    _Logger.LogWarning($"error when pinging lobby: {pong.Error}");
                } else {
                    _Logger.LogInformation($"got PONG from lobby host, Gex is still connected");
                }
            }
        }

        private LobbyMessage? parseMessage(List<byte> buffer) {
            LobbyMessage msg = new();

            //_Logger.LogTrace($"parse started buffer={string.Join("", buffer.Select(iter => (char)iter))}");

            int i = 0;

            if (buffer[0] == '#') {
                //_Logger.LogTrace($"parse started buffer=" +
                //    $"\n{string.Join("", buffer.Select(iter => (char)iter))}" +
                //    $"\n{string.Join(" ", buffer.Select(iter => iter))}");

                string msgId = "";
                ++i; // skip the #
                for (; i < buffer.Count; ++i) {
                    if (buffer[i] == ' ') {
                        ++i; // advance i once more to read beyond the space
                        break;
                    }

                    msgId += (char)buffer[i];
                    //_Logger.LogTrace($"message id character read [msgId={msgId}] [i={i}]");
                }

                if (i >= buffer.Count) {
                    _Logger.LogTrace($"failed to read message ID response [msgId={msgId}] [i={i}] [buffer.Count={buffer.Count}]");
                    return null;
                }

                msg.MessageId = int.Parse(msgId);
            }

            for (; i < buffer.Count; ++i) {
                if (buffer[i] == ' ' || buffer[i] == '\n') {
                    //_Logger.LogTrace($"parsed message command [command={msg.Command}]");
                    if (buffer[i] == ' ') {
                        ++i; // advance i one more to remove the space from the argument
                    }
                    break;
                }

                //_Logger.LogTrace($"iter [i={i}] [msg.Command={msg.Command}]");
                msg.Command += (char)buffer[i];
            }

            if (i < buffer.Count && buffer[i] == '\n') {
                buffer.RemoveRange(0, i + 1); // +1 to remove the newline
                return msg;
            }

            // haven't reached the end of the message yet, need a \n for that
            if (i >= buffer.Count) {
                //_Logger.LogTrace($"no arguments in TCP yet [i={i}] [cmd={msg.Command}] [buffer.Count={buffer.Count}]");
                return null;
            }

            string args = "";
            for (; i < buffer.Count; ++i) {
                if (buffer[i] == '\n') {
                    //_Logger.LogTrace($"argument [arg={msg.Arguments}]");
                    break;
                }

                args += (char)buffer[i];
            }
            msg.Arguments = args;

            if (i >= buffer.Count) {
                //_Logger.LogTrace($"no new line, arguments is not done [i={i}] [buffer.Count={buffer.Count}]");
                return null;
            }

            buffer.RemoveRange(0, i + 1); // +1 to remove the newline

            return msg;
        }

    }
}
