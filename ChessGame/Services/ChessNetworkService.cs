using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ChessGame.Models;

namespace ChessGame.Services
{
    public class ChessNetworkService : INetworkService
    {
        private TcpListener? _server;
        private List<TcpClient> _clients = new List<TcpClient>();
        private TcpClient? _localClient; // Used when this instance is a client connecting to a host
        private CancellationTokenSource? _cts;

        public bool IsHost { get; private set; }
        
        public event Action<NetworkMessage, object?>? OnMessageReceived;
        public event Action<object>? OnClientConnected;
        public event Action<object>? OnClientDisconnected;

        // Host Methods
        public void StartHost(int port)
        {
            IsHost = true;
            _cts = new CancellationTokenSource();
            _server = new TcpListener(IPAddress.Any, port);
            _server.Start();

            Task.Run(async () =>
            {
                try
                {
                    while (!_cts.Token.IsCancellationRequested)
                    {
                        var client = await _server.AcceptTcpClientAsync();
                        _clients.Add(client);
                        OnClientConnected?.Invoke(client);
                        _ = HandleClientAsync(client, _cts.Token);
                    }
                }
                catch (ObjectDisposedException) { }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Host error: {ex.Message}");
                }
            }, _cts.Token);
        }

        // Client Methods
        public async Task<bool> ConnectAsync(string ip, int port)
        {
            IsHost = false;
            _cts = new CancellationTokenSource();
            _localClient = new TcpClient();
            try
            {
                await _localClient.ConnectAsync(ip, port);
                _ = HandleClientAsync(_localClient, _cts.Token);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Shared Methods
        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            try
            {
                using var reader = new StreamReader(client.GetStream());
                while (!token.IsCancellationRequested)
                {
                    string? line = await reader.ReadLineAsync();
                    if (line == null) break; // Disconnected

                    var msg = JsonSerializer.Deserialize<NetworkMessage>(line);
                    if (msg != null)
                    {
                        OnMessageReceived?.Invoke(msg, client);
                    }
                }
            }
            catch (Exception)
            {
                // Client disconnected abruptly
            }
            finally
            {
                if (IsHost)
                {
                    _clients.Remove(client);
                }
                OnClientDisconnected?.Invoke(client);
                client.Close();
            }
        }

        // Send to Server (if Client) or Send to specific client (if Host)
        public async Task SendMessageAsync(NetworkMessage message, object? specificClient = null)
        {
            string json = JsonSerializer.Serialize(message) + "\n"; // Newline delimits messages
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(json);

            if (IsHost)
            {
                var targetClient = specificClient as TcpClient;
                if (targetClient != null && targetClient.Connected)
                {
                    await targetClient.GetStream().WriteAsync(buffer, 0, buffer.Length);
                }
            }
            else
            {
                if (_localClient != null && _localClient.Connected)
                {
                    await _localClient.GetStream().WriteAsync(buffer, 0, buffer.Length);
                }
            }
        }

        // Host only: Broadcast to all clients
        public async Task BroadcastMessageAsync(NetworkMessage message, object? excludeClient = null)
        {
            if (!IsHost) return;

            string json = JsonSerializer.Serialize(message) + "\n";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(json);

            var disconnectedClients = new List<TcpClient>();

            foreach (var client in _clients)
            {
                if (client == (excludeClient as TcpClient)) continue;

                if (client.Connected)
                {
                    try
                    {
                        await client.GetStream().WriteAsync(buffer, 0, buffer.Length);
                    }
                    catch
                    {
                        disconnectedClients.Add(client);
                    }
                }
                else
                {
                    disconnectedClients.Add(client);
                }
            }

            foreach (var dc in disconnectedClients)
            {
                _clients.Remove(dc);
                OnClientDisconnected?.Invoke(dc);
            }
        }

        public void StopAll()
        {
            _cts?.Cancel();
            
            if (_server != null)
            {
                _server.Stop();
                _server = null;
            }

            foreach (var client in _clients)
            {
                client.Close();
            }
            _clients.Clear();

            _localClient?.Close();
            _localClient = null;
        }
    }
}
