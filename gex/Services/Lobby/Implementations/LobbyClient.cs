using gex.Models;
using gex.Models.Lobby;
using gex.Models.Options;
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
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Lobby.Implementations {

    public class LobbyClient : ILobbyClient {

        private readonly ILogger<LobbyClient> _Logger;
        private readonly IOptions<SpringLobbyOptions> _Options;
        private readonly BaseQueue<LobbyMessage> _LobbyMessageQueue;

        private readonly TcpClient _TcpSocket;
        private static int _MessageId = 1;
        private readonly Dictionary<int, LobbyMessage> _PendingMessages = [];

        public LobbyClient(ILogger<LobbyClient> logger,
            IOptions<SpringLobbyOptions> options, BaseQueue<LobbyMessage> lobbyMessageQueue) {

            _TcpSocket = new TcpClient();

            _Logger = logger;
            _Options = options;
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

        public async Task<Result<bool, string>> Connect(CancellationToken cancel) {
            if (_TcpSocket.Connected == true) {
                _Logger.LogInformation($"lobby client is already connected, reconnecting");
                _TcpSocket.Close();
            }

            try {
                _Logger.LogInformation($"lobby client connecting to host [host={_LobbyHost}:{_LobbyPort}]");
                await _TcpSocket.ConnectAsync(_LobbyHost, _LobbyPort, cancel);

                Thread readThread = new(() => {
                    try {
                        ReadThread(_TcpSocket, cancel);
                    } catch (Exception) when (cancel.IsCancellationRequested == true) {
                        _Logger.LogInformation($"closing TCP client");
                    } catch (Exception ex) when (cancel.IsCancellationRequested == false) {
                        _Logger.LogError(ex, $"error in readThread");
                    }
                });
                readThread.Start();

                Thread pingWriteThread = new(() => {
                    try {
                        PingWriteThread(cancel);
                    } catch (Exception) when (cancel.IsCancellationRequested == true) {
                        _Logger.LogInformation($"closing ping write thread safely due to cancellation token");
                    } catch (Exception ex) when (cancel.IsCancellationRequested == false) {
                        _Logger.LogError(ex, "error in ping write thread");
                    }
                });
                pingWriteThread.Start();

                CancellationTokenRegistration cancelCallback = cancel.Register(() => {
                    _Logger.LogInformation($"joining TCP read thread");
                    readThread.Join();

                    _Logger.LogInformation($"joining ping write thread");
                    pingWriteThread.Join();
                });
            } catch (Exception ex) {
                _Logger.LogError(ex, "failed to connect to host");
                return ex.Message;
            }

            return true;
        }

        public async Task<Result<bool, string>> Disconnect(CancellationToken cancel) {
            if (_TcpSocket.Connected == false) {
                return true;
            }

            await Write("EXIT", cancel);

            _TcpSocket.Close();
            return true;
        }

        public async Task<Result<LobbyMessage, string>> Login(CancellationToken cancel) {
            if (_TcpSocket.Connected == false) {
                return "TCP socket is not connected";
            }

            _Logger.LogDebug($"logging in to Spring lobby [host={_LobbyHost}:{_LobbyPort}] [user={_Options.Value.Username}]");

            byte[] md5 = MD5.HashData(Encoding.UTF8.GetBytes(_Options.Value.Password));
            string password = Convert.ToBase64String(md5);

            Result<LobbyMessage, string> loginRequest = await WriteReply(
                $"LOGIN {_Options.Value.Username} {password} 0 * LuaLobby Chobby\t:3 :3\tb sp", TimeSpan.FromSeconds(5), cancel
            );

            return loginRequest;
        }

        public async Task<Result<bool, string>> Write(string message, CancellationToken cancel) {
            byte[] msg = Encoding.UTF8.GetBytes(message + "\n");
            try {
                await _TcpSocket.GetStream().WriteAsync(msg, cancel);
            } catch (Exception ex) {
                _Logger.LogError(ex, "failed to write LOGIN command");
                return ex.Message;
            }

            return true;
        }

        public async Task<Result<LobbyMessage, string>> WriteReply(string message, TimeSpan timeout, CancellationToken cancel) {
            int msgId = _MessageId++;
            byte[] msg = Encoding.UTF8.GetBytes($"#{msgId} {message}\n");
            try {
                await _TcpSocket.GetStream().WriteAsync(msg, cancel);
            } catch (Exception ex) {
                _Logger.LogError(ex, "failed to write LOGIN command");
                return ex.Message;
            }

            DateTime timeoutEnd = DateTime.UtcNow + timeout;

            LobbyMessage? response = null;
            while (response == null) {
                response = _PendingMessages.GetValueOrDefault(msgId);

                if (response != null) {
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

                int amt = stream.Read(buffer, 0, 1024);
                message.AddRange(buffer.AsSpan()[..amt]);

                //_Logger.LogTrace($"tcp read [amt={amt}] [str={string.Join("", message.Select(iter => (char)iter))}]");

                while (cancel.IsCancellationRequested == false) {
                    cancel.ThrowIfCancellationRequested();

                    LobbyMessage? msg = parseMessage(message);
                    if (msg == null) {
                        break;
                    }

                    //_Logger.LogDebug($"RECV || {msg.Command}: {msg.Arguments}");

                    if (msg.MessageId != null) {
                        if (_PendingMessages.ContainsKey(msg.MessageId.Value) == true) {
                            _Logger.LogWarning($"colliding message ID response from server found [messageID={msg.MessageId}] [command={msg.Command}]");
                        } else {
                            _PendingMessages.Add(msg.MessageId.Value, msg);
                            _Logger.LogTrace($"added message id response [messageID={msg.MessageId}] [command={msg.Command}]");
                        }
                    } else {
                        _LobbyMessageQueue.Queue(msg);
                    }

                    if (message.Count == 0) {
                        break;
                    }
                }
            }
        }

        private async void PingWriteThread(CancellationToken cancel) {
            _Logger.LogInformation($"ping write started");

            while (cancel.IsCancellationRequested == false) {
                // delay at the start so PING is sent 30 seconds after login
                await Task.Delay(TimeSpan.FromSeconds(30), cancel);

                Result<LobbyMessage, string> pong = await WriteReply("PING", TimeSpan.FromSeconds(5), cancel);
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
                /*
                _Logger.LogTrace($"parse started buffer=" +
                    $"\n{string.Join("", buffer.Select(iter => (char)iter))}" +
                    $"\n{string.Join(" ", buffer.Select(iter => iter))}");
                */

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
