using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class ChatClient
{
    private readonly TcpClient _client;
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;

    public event Action<string>? MessageReceived;

    public ChatClient(string serverAddress, int port)
    {
        _client = new TcpClient();
        _client.Connect(serverAddress, port);
        var stream = _client.GetStream();
        _reader = new StreamReader(stream, Encoding.UTF8);
        _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
    }

    public async Task StartReceivingAsync()
    {
        while (_client.Connected)
        {
            try
            {
                var message = await _reader.ReadLineAsync();
                if (message != null)
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
            var request = $"{action}:{username}:{password}";
            await _writer.WriteLineAsync(request);
            var response = await _reader.ReadLineAsync();
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

    public void Disconnect()
    {
        _client.Close();
    }
}
