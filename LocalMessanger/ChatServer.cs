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

public class ChatServer
{
    private readonly TcpListener _listener;
    private readonly ConcurrentDictionary<TcpClient, string> _clients = new();
    // Store the writer for each connected client.
    private readonly ConcurrentDictionary<TcpClient, StreamWriter> _clientWriters = new();
    private readonly LogService _logService;
    private readonly AuthService _authService;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isStopped = false;

    public ObservableCollection<string> ConnectedUsers { get; } = new();

    public ChatServer(int port, LogService logService)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        _logService = logService;
        _authService = new AuthService(new AppDbContext(), _logService);
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
                try
                {
                    if (!_listener.Server.IsBound)
                        break;

                    var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                    _ = HandleClientAsync(client, cancellationToken);
                }
                catch (OperationCanceledException) { break; }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex)
                {
                    if (_listener.Server.IsBound)
                        _logService.Log($"Error accepting client: {ex.Message}", "Error");
                }
            }
        }
        finally
        {
            Stop();
        }
    }

    public void Stop()
    {
        if (_isStopped) return;
        _isStopped = true;
        _cancellationTokenSource?.Cancel();
        _listener.Stop();

        foreach (var client in _clients.Keys)
        {
            client.Close();
        }

        _logService.Log("Server stopped", "Info");
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        // Get initial identifier (IP:port); may be replaced after login.
        string initialId = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
        // Add client to dictionary with this initial identifier.
        _clients.TryAdd(client, initialId);
        App.Current.Dispatcher.Invoke(() => ConnectedUsers.Add(initialId));
        _logService.Log($"Client connected: {initialId}", "Info");

        try
        {
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            // Create a writer for this client and store it for later use.
            var writer = new StreamWriter(stream, Encoding.UTF8)
            {
                AutoFlush = true
            };
            _clientWriters[client] = writer;

            while (!cancellationToken.IsCancellationRequested)
            {
                var request = await reader.ReadLineAsync();
                if (request == null)
                    break;

                _logService.Log($"Received request: {request}", "Debug");
                var response = await ProcessRequestAsync(request, client);
                await writer.WriteLineAsync(response);
            }
        }
        catch (IOException ioex)
        {
            // Likely the connection was closed by the client.
            _logService.Log($"IOException handling client ({initialId}): {ioex.Message}", "Error");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logService.Log($"Error handling client ({initialId}): {ex.Message}", "Error");
        }
        finally
        {
            // Retrieve the current identifier for removal.
            string currentId;
            if (!_clients.TryGetValue(client, out currentId))
                currentId = initialId;

            _clients.TryRemove(client, out _);
            _clientWriters.TryRemove(client, out _);
            App.Current.Dispatcher.Invoke(() =>
            {
                if (ConnectedUsers.Contains(currentId))
                    ConnectedUsers.Remove(currentId);
            });
            _logService.Log($"Client disconnected: {currentId}", "Info");
            client.Close();
        }
    }

    private async Task<string> ProcessRequestAsync(string request, TcpClient client)
    {
        try
        {
            var parts = request.Split(':');
            // For login / register, expect at least 3 parts:
            if (parts.Length < 3)
                return "error:invalid_request";

            var action = parts[0];

            if (action == "register")
            {
                var username = parts[1];
                var password = parts[2];
                var success = await _authService.RegisterAsync(username, password);
                return success ? "success" : "error:username_taken";
            }
            else if (action == "login")
            {
                var username = parts[1];
                var password = parts[2];
                var success = await _authService.LoginAsync(username, password);
                if (success)
                {
                    // Update the client's identifier to be the username.
                    _clients[client] = username;
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        // Remove the old (IP:port) entry and add the username.
                        var old = client.Client.RemoteEndPoint?.ToString();
                        if (old != null && ConnectedUsers.Contains(old))
                        {
                            ConnectedUsers.Remove(old);
                        }
                        ConnectedUsers.Add(username);
                    });
                }
                return success ? "success" : "error:invalid_credentials";
            }
            else if (action == "message")
            {
                // For messages, expect at least 4 parts: action, sender, receiver, content
                if (parts.Length < 4)
                    return "error:invalid_message_format";
                var sender = parts[1];
                var receiver = parts[2];
                var content = string.Join(":", parts.Skip(3));

                await _logService.LogAsync($"Message from {sender} to {receiver}: {content}", "Info");

                // Save the message to the database.
                using (var db = new AppDbContext())
                {
                    var senderUser = db.Users.FirstOrDefault(u => u.Username == sender);
                    var receiverUser = db.Users.FirstOrDefault(u => u.Username == receiver);
                    if (senderUser == null || receiverUser == null)
                        return "error:user_not_found";

                    var msg = new Message
                    {
                        SenderId = senderUser.Id,
                        ReceiverId = receiverUser.Id,
                        Text = content,
                        SentAt = DateTime.UtcNow
                    };
                    db.Messages.Add(msg);
                    await db.SaveChangesAsync();
                }

                // Forward the message to the receiver if connected.
                foreach (var kvp in _clients.ToList())
                {
                    if (kvp.Value == receiver)
                    {
                        try
                        {
                            // Use the already created StreamWriter for the receiver.
                            if (_clientWriters.TryGetValue(kvp.Key, out var receiverWriter))
                            {
                                await receiverWriter.WriteLineAsync($"message:{sender}:{content}");
                                return "success";
                            }
                        }
                        catch (Exception ex)
                        {
                            _logService.Log($"Error forwarding message to {receiver}: {ex.Message}", "Error");
                            _clients.TryRemove(kvp.Key, out _);
                            _clientWriters.TryRemove(kvp.Key, out _);
                            App.Current.Dispatcher.Invoke(() =>
                            {
                                if (ConnectedUsers.Contains(kvp.Value))
                                    ConnectedUsers.Remove(kvp.Value);
                            });
                        }
                    }
                }
                return "error:receiver_not_found";
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
