using LocalMessangerServer.EF;
using LocalMessangerServer;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LocalMessanger;
using Microsoft.EntityFrameworkCore;

namespace LocalMessangerServer
{
    public class ChatServer
    {
        private readonly TcpListener _listener;
        private readonly ConcurrentDictionary<TcpClient, StreamWriter> _clientWriters = new();
        private readonly ConcurrentDictionary<string, StreamWriter> _userWriters = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<TcpClient, (string Username, UserStatus Status)> _clients = new();
        private readonly LogService _logService;
        private readonly AuthService _authService;
        private readonly AppDbContext _db;
        private bool _isStopped = false;

        public ObservableCollection<string> ConnectedUsers { get; } = new();

        public ChatServer(int port, LogService logService)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _logService = logService;
            _db = new AppDbContext();
            _authService = new AuthService(_db, _logService);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _listener.Start();
            _isStopped = false;
            _logService.Log("Server started", "Info");

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                    _ = HandleClientAsync(client, cancellationToken);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                Stop();
            }
        }

        public void Stop()
        {
            if (_isStopped) return;
            _isStopped = true;
            _listener.Stop();

            foreach (var client in _clients.Keys)
                client.Close();

            _logService.Log("Server stopped", "Info");
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            string connectionId = client.Client.RemoteEndPoint?.ToString() ?? Guid.NewGuid().ToString();
            _clients[client] = (connectionId, UserStatus.Offline);
            App.Current.Dispatcher.Invoke(() => ConnectedUsers.Add(connectionId));
            _logService.Log($"Client connected: {connectionId}", "Info");

            try
            {
                using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
                _clientWriters[client] = writer;

                while (!cancellationToken.IsCancellationRequested)
                {
                    var request = await reader.ReadLineAsync();
                    if (request == null) break;

                    _logService.Log($"Received request: {request}", "Debug");
                    var response = await ProcessRequestAsync(request, client);
                    await writer.WriteLineAsync(response);
                }
            }
            catch (IOException ex)
            {
                _logService.Log($"IOException ({connectionId}): {ex.Message}", "Error");
            }
            finally
            {
                if (_clients.TryRemove(client, out var info))
                {
                    _userWriters.TryRemove(info.Username, out _);
                    App.Current.Dispatcher.Invoke(() => ConnectedUsers.Remove(info.Username));
                }
                _clientWriters.TryRemove(client, out _);
                client.Close();
                _logService.Log($"Client disconnected: {connectionId}", "Info");
            }
        }

        private async Task<string> ProcessRequestAsync(string request, TcpClient client)
        {
            try
            {
                var parts = request.Split(':', 4);
                var action = parts[0];

                switch (action)
                {
                    case "get_salt" when parts.Length == 2:
                        var user = _db.Users.FirstOrDefault(x => x.Username == parts[1]);
                        return user != null ? user.PasswordHash : "error:user_not_found";

                    case "list_users":
                        var users = _db.Users.Select(u => u.Username).ToArray();
                        return string.Join(",", users);

                    case "get_history" when parts.Length == 3:
                        var userA = parts[1];
                        var userB = parts[2];
                        var msgs = _db.Messages
                            .Where(m => (m.Sender.Username == userA && m.Receiver.Username == userB)
                                     || (m.Sender.Username == userB && m.Receiver.Username == userA))
                            .OrderBy(m => m.SentAt)
                            .Select(m => $"{m.Sender.Username}:{m.Text}|{m.SentAt:o}")
                            .ToArray();
                        return string.Join("|", msgs);

                    case string a when a == "register" && parts.Length >= 3:
                        return await _authService.RegisterAsync(parts[1], parts[2])
                            ? "success" : "error:username_taken";

                    case string a when a == "login" && parts.Length >= 3:
                        if (await _authService.LoginAsync(parts[1], parts[2]))
                        {
                            
                            _clients[client] = (parts[1], UserStatus.Online);
                            _userWriters[parts[1]] = _clientWriters[client];
                            App.Current.Dispatcher.Invoke(() =>
                            {
                                ConnectedUsers.Remove(client.Client.RemoteEndPoint?.ToString());
                                ConnectedUsers.Add(parts[1]);
                            });
                            return "success";
                        }
                        return "error:invalid_credentials";

                    case string a when a == "block" && parts.Length >= 3:
                        var blocker = _db.Users.FirstOrDefault(u => u.Username == parts[1]);
                        var toBlock = _db.Users.FirstOrDefault(u => u.Username == parts[2]);
                        if (blocker == null || toBlock == null) return "error:user_not_found";
                        if (!_db.Blocks.Any(b => b.BlockerId == blocker.Id && b.BlockedId == toBlock.Id))
                        {
                            _db.Blocks.Add(new Block { BlockerId = blocker.Id, BlockedId = toBlock.Id, CreatedAt = DateTime.UtcNow });
                            await _db.SaveChangesAsync();
                        }
                        return "success";

                    case string a when a == "status" && parts.Length >= 3:
                        if (!Enum.TryParse<UserStatus>(parts[2], true, out var status))
                            return "error:invalid_status";

                        if (_clients.TryGetValue(client, out var info))
                        {
                            _clients[client] = (info.Username, status);
                            App.Current.Dispatcher.Invoke(() =>
                            {
                                ConnectedUsers.Remove(info.Username);
                                ConnectedUsers.Add(info.Username);
                            });
                        
                            foreach (var writer in _userWriters.Values)
                                await writer.WriteLineAsync($"notify_status:{info.Username}:{status}");
                            await _logService.LogAsync($"User {info.Username} changed status to {status}", "Debug");
                            return "success";
                        }
                        return "error:client_not_found";

                    case string a when a == "message" && parts.Length >= 4:
                        var sender = parts[1];
                        var receiver = parts[2];
                        var content = parts[3];
                        await _logService.LogAsync($"Message from {sender} to {receiver}: {content}", "Info");

                        var snd = _db.Users.FirstOrDefault(u => u.Username == sender);
                        var rcv = _db.Users.FirstOrDefault(u => u.Username == receiver);
                        if (snd == null || rcv == null) return "error:user_not_found";
                        if (_db.Blocks.Any(b => b.BlockerId == rcv.Id && b.BlockedId == snd.Id))
                            return "error:blocked";

                        _db.Messages.Add(new Message { SenderId = snd.Id, ReceiverId = rcv.Id, Text = content, SentAt = DateTime.UtcNow });
                        await _db.SaveChangesAsync();

                        if (_userWriters.TryGetValue(receiver, out var rw))
                        {
                            await rw.WriteLineAsync($"message:{sender}:{content}|{DateTime.UtcNow:o}");
                            return "success";
                        }
                        return "offline";

                    default:
                        return "error:unknown_action";
                }
            }
            catch (Exception ex)
            {
                _logService.Log($"Error processing request: {ex.Message}", "Error");
                return "error:server_error";
            }
        }
    }
}
