using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using ChessGame.Models;

namespace ChessGame.Services
{
    public class SignalRNetworkService : INetworkService
    {
        private HubConnection? _hubConnection;
        private string _roomId = "";
        private string _playerName = "";

        public bool IsHost { get; private set; }

        public event Action<NetworkMessage, object?>? OnMessageReceived;
        public event Action<object>? OnClientConnected;
        public event Action<object>? OnClientDisconnected;

        // In SignalR, we don't start a local server. We just connect and act as Host or Client via logic.
        public async Task StartHostAsync(string serverUrl, string roomId, string hostName)
        {
            IsHost = true;
            _roomId = roomId;
            _playerName = hostName;
            
            await ConnectAndJoinAsync(serverUrl, roomId, hostName, true);
        }

        public async Task<bool> ConnectAsync(string serverUrl, string roomId, string playerName)
        {
            IsHost = false;
            _roomId = roomId;
            _playerName = playerName;

            try
            {
                await ConnectAndJoinAsync(serverUrl, roomId, playerName, false);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task ConnectAndJoinAsync(string url, string roomId, string playerName, bool isHost)
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(url)
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.On<NetworkMessage, string>("ReceiveMessage", (msg, senderId) =>
            {
                if (msg.Type == MessageType.PlayerDisconnected)
                {
                    OnClientDisconnected?.Invoke(senderId);
                }
                else
                {
                    OnMessageReceived?.Invoke(msg, senderId);
                }
            });

            await _hubConnection.StartAsync();
            await _hubConnection.InvokeAsync("JoinRoom", roomId, playerName, isHost);

            // Notify connected (though in SignalR it's group based, we can just trigger it)
            if (isHost) OnClientConnected?.Invoke("host_self");
            else OnClientConnected?.Invoke("client_self");
        }

        public async Task SendMessageAsync(NetworkMessage message, object? specificClient = null)
        {
            if (_hubConnection == null) return;

            if (specificClient is string targetId && !string.IsNullOrEmpty(targetId))
            {
                await _hubConnection.InvokeAsync("SendMessageToUser", targetId, message);
            }
            else
            {
                // Send to server (if we're client and just want to broadcast)
                await _hubConnection.InvokeAsync("SendMessage", _roomId, message);
            }
        }

        public async Task BroadcastMessageAsync(NetworkMessage message, object? excludeClient = null)
        {
            if (!IsHost || _hubConnection == null) return;
            // Exclude is not trivially supported without passing it to hub, but we can just broadcast.
            // Our hub uses OthersInGroup, so it already excludes sender (Host).
            await _hubConnection.InvokeAsync("SendMessage", _roomId, message);
        }

        public void StopAll()
        {
            if (_hubConnection != null)
            {
                _hubConnection.InvokeAsync("LeaveRoom", _roomId).Wait(1000);
                _hubConnection.StopAsync().Wait(1000);
                _hubConnection.DisposeAsync().AsTask().Wait(1000);
                _hubConnection = null;
            }
        }
    }
}
