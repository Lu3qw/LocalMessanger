using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

public class ChatClient
{   
    private readonly TcpClient _client;
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;

    public event Action<string>? MessageReceived;

    private readonly ConcurrentQueue<TaskCompletionSource<string>> _responseQueue = new();

    public ChatClient(string serverAddress, int port)
    {
        _client = new TcpClient();
        _client.Connect(serverAddress, port);
        var stream = _client.GetStream();
        _reader = new StreamReader(stream, Encoding.UTF8);
        _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

        
        _ = StartReceivingAsync();
    }

    private async Task StartReceivingAsync()
    {
        while (_client.Connected)
        {
            try
            {
                var message = await _reader.ReadLineAsync();
                if (message == null)
                    break;

                if (_responseQueue.TryDequeue(out var tcs))
                {
                    tcs.TrySetResult(message);
                }
                else
                {
                    MessageReceived?.Invoke(message);
                }
            }
            catch
            {
                break;
            }
        }
    }

    public async Task<string> SendAuthRequestAsync(string action, string username, string password)
    {
        if (_client.Connected)
        {
            var tcs = new TaskCompletionSource<string>();
            _responseQueue.Enqueue(tcs);

            var request = $"{action}:{username}:{password}";
            await _writer.WriteLineAsync(request);

            var response = await tcs.Task;
            return response ?? "error";
        }
        return "error";
    }

    public async Task SendMessageAsync(string sender, string receiver, string content)
    {
        if (_client.Connected)
        {
            var message = $"message:{sender}:{receiver}:{content}";
            await _writer.WriteLineAsync(message);
        }
    }

    public async Task<string> SendRequestAsync(string req)
    {
        var tcs = new TaskCompletionSource<string>();
        _responseQueue.Enqueue(tcs);
        await _writer.WriteLineAsync(req);
        return await tcs.Task;
    }

    public Task<string> BlockUserAsync(string me, string toBlock)
  => SendRequestAsync($"block:{me}:{toBlock}");

    public Task<string> ChangeStatusAsync(string user, string status)
      => SendRequestAsync($"status:{user}:{status}");



    public void Disconnect()
    {
        _client.Close();
    }
}
