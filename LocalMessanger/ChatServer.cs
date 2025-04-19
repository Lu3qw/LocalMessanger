using LocalMessangerServer;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Text;
using LocalMessanger;

public class ChatServer
{
    private readonly TcpListener _listener;
    private readonly ConcurrentDictionary<TcpClient, string> _clients = new();
    private readonly LogService _logService;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isStopped = false; // Додано прапорець для перевірки стану сервера

    public ObservableCollection<string> ConnectedUsers { get; } = new();

    public ChatServer(int port, LogService logService)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        _logService = logService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _listener.Start();
        _isStopped = false; // Скидаємо прапорець при старті
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
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (_listener.Server.IsBound)
                    {
                        _logService.Log($"Error accepting client: {ex.Message}", "Error");
                    }
                }
            }
        }
        finally
        {
            // Викликаємо Stop лише якщо сервер ще не зупинений
            if (!_isStopped)
            {
                Stop();
            }
        }
    }

    public void Stop()
    {
        if (_isStopped) return; // Перевіряємо, чи сервер вже зупинений

        _isStopped = true; // Встановлюємо прапорець
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
        var clientEndPoint = client.Client.RemoteEndPoint?.ToString();
        if (clientEndPoint != null)
        {
            _clients.TryAdd(client, clientEndPoint);
            App.Current.Dispatcher.Invoke(() => ConnectedUsers.Add(clientEndPoint));
            _logService.Log($"Client connected: {clientEndPoint}", "Info");
        }

        try
        {
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            while (!cancellationToken.IsCancellationRequested)
            {
                var message = await reader.ReadLineAsync();
                if (message == null) break;

                _logService.Log($"Message received from {clientEndPoint}: {message}", "Info");
                BroadcastMessage($"{clientEndPoint}: {message}");
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logService.Log($"Error handling client {clientEndPoint}: {ex.Message}", "Error");
        }
        finally
        {
            if (clientEndPoint != null)
            {
                _clients.TryRemove(client, out _);
                App.Current.Dispatcher.Invoke(() => ConnectedUsers.Remove(clientEndPoint));
                _logService.Log($"Client disconnected: {clientEndPoint}", "Info");
            }

            client.Close();
        }
    }

    private void BroadcastMessage(string message)
    {
        foreach (var client in _clients.Keys)
        {
            try
            {
                using var writer = new StreamWriter(client.GetStream(), Encoding.UTF8) { AutoFlush = true };
                writer.WriteLine(message);
            }
            catch
            {
                // Ігнорувати помилки для відключених клієнтів
            }
        }
    }
}
