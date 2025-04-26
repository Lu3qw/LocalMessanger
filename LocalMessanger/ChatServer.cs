
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
using System.Reflection.Metadata;

namespace LocalMessangerServer
{
    public class ChatServer
    {
        private readonly TcpListener _listener;
        private readonly ConcurrentDictionary<TcpClient, StreamWriter> _clientWriters = new();
        private readonly LogService _logService;
        private readonly AuthService _authService;
        private readonly AppDbContext _db;
        private bool _isStopped = false;


        private readonly ConcurrentDictionary<TcpClient, (string Username, UserStatus Status)> _clients = new ConcurrentDictionary<TcpClient, (string, UserStatus)>();
        public ObservableCollection<string> ConnectedUsers { get; } = new ObservableCollection<string>();


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
            string initialId = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
            _clients[client] = (initialId, UserStatus.Offline); // Fix: Assign a valid tuple with Username and Status
            App.Current.Dispatcher.Invoke(() => ConnectedUsers.Add(initialId));
            _logService.Log($"Client connected: {initialId}", "Info");

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
                _logService.Log($"IOException ({initialId}): {ex.Message}", "Error");
            }
            finally
            {
                _clients.TryRemove(client, out var id);
                _clientWriters.TryRemove(client, out _);
                App.Current.Dispatcher.Invoke(() =>
                {
                    if (ConnectedUsers.Contains(id.Username))
                        ConnectedUsers.Remove(id.Username);
                });
                client.Close();
                _logService.Log($"Client disconnected: {id}", "Info");
            }
        }

        private async Task<string> ProcessRequestAsync(string request, TcpClient client)
        {
            try
            {
                var parts = request.Split(':');
                var action = parts[0];


                if (action == "get_salt" && parts.Length == 2)
                {
                    var u = parts[1];
                    var user = _db.Users.FirstOrDefault(x => x.Username == u);
                    return user != null
                        ? user.PasswordHash            
                        : "error:user_not_found";
                }


                
                if (action == "list_users")
                {
                    var users = _db.Users.Select(u => u.Username).ToArray();
                    return string.Join(",", users);
                }

                if (action == "get_history" && parts.Length == 3)
                {
                    var userA = parts[1];
                    var userB = parts[2];
                    var msgs = _db.Messages
                        .Where(m => (m.Sender.Username == userA && m.Receiver.Username == userB)
                                 || (m.Sender.Username == userB && m.Receiver.Username == userA))
                        .OrderBy(m => m.SentAt)
                        .Select(m => $"{m.Sender.Username}:{m.Text}|{m.SentAt:o}")
                        .ToArray();
                    return string.Join("|", msgs);
                }

                
                if (parts.Length < 3)
                    return "error:invalid_request";

                if (action == "register")
                {
                    var username = parts[1];
                    var passwordHash = parts[2];
                    var success = await _authService.RegisterAsync(username, passwordHash);
                    return success ? "success" : "error:username_taken";
                }
                else if (action == "login")
                {
                    var username = parts[1];
                    var passwordHash = parts[2];
                    var success = await _authService.LoginAsync(username, passwordHash);
                    if (success)
                    {
                        _clients[client] = (username, UserStatus.Online);
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            ConnectedUsers.Remove(client.Client.RemoteEndPoint?.ToString());
                            ConnectedUsers.Add(username);
                        });
                    }

                    return success ? "success" : "error:invalid_credentials";
                }
                else if (action == "block" && parts.Length == 3)
                {
                    var blocker = parts[1];
                    var toBlock = parts[2];
                    var u1 = _db.Users.FirstOrDefault(u => u.Username == blocker);
                    var u2 = _db.Users.FirstOrDefault(u => u.Username == toBlock);
                    if (u1 == null || u2 == null) return "error:user_not_found";
                    if (!_db.Blocks.Any(b => b.BlockerId == u1.Id && b.BlockedId == u2.Id))
                    {
                        _db.Blocks.Add(new Block { BlockerId = u1.Id, BlockedId = u2.Id, CreatedAt = DateTime.UtcNow });
                        await _db.SaveChangesAsync();
                    }
                    return "success";
                }
                if (action == "status")
                {
                    if (parts.Length < 3)
                        return "error:invalid_status_format";
                    var username = parts[1];
                    var statusStr = parts[2];
                    if (!Enum.TryParse<UserStatus>(statusStr, true, out var status))
                        return "error:invalid_status";
                    if (_clients.TryGetValue(client, out var clientInfo))
                    {
                        _clients[client] = (username, status);
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            ConnectedUsers.Remove(clientInfo.Username);
                            ConnectedUsers.Add(username);
                        });
                    }
                    else
                    {
                        return "error:client_not_found";
                    }


                    await _logService.LogAsync($"User {username} changed status to {statusStr} ", "Debug");
                    return "";

                }
                else if (action == "message")
                {
                    if (parts.Length < 4)
                        return "error:invalid_message_format";

                    var sender = parts[1];
                    var receiver = parts[2];
                    var content = string.Join(":", parts.Skip(3));

                    await _logService.LogAsync($"Message from {sender} to {receiver}: {content}", "Info");


                    var senderUser = _db.Users.FirstOrDefault(u => u.Username == sender);
                    var receiverUser = _db.Users.FirstOrDefault(u => u.Username == receiver);
                    if (senderUser == null || receiverUser == null)
                        return "error:user_not_found";

                    var blocked = _db.Blocks.Any(b => b.BlockerId == receiverUser.Id && b.BlockedId == senderUser.Id);
                    if (blocked) return "error:blocked";

                    var msg = new Message
                    {
                        SenderId = senderUser.Id,
                        ReceiverId = receiverUser.Id,
                        Text = content,
                        SentAt = DateTime.UtcNow
                    };
                    _db.Messages.Add(msg);
                    await _db.SaveChangesAsync();


                    foreach (var kvp in _clients.ToList())
                    {
                        if (kvp.Value.Username == receiver && _clientWriters.TryGetValue(kvp.Key, out var w))
                        {
                            await w.WriteLineAsync($"message:{sender}:{content}|{DateTime.UtcNow}");
                            return "success";
                        }
                    }
                    return "offline";
                }
                else
                {
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
