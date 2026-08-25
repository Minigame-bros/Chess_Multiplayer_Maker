using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using ChessGame.Models;

namespace ChessGame.Services
{
    public interface INetworkService
    {
        bool IsHost { get; }
        
        // Use object instead of TcpClient for client identification to support both TCP and SignalR
        event Action<NetworkMessage, object?>? OnMessageReceived;
        event Action<object>? OnClientConnected;
        event Action<object>? OnClientDisconnected;

        Task SendMessageAsync(NetworkMessage message, object? specificClient = null);
        Task BroadcastMessageAsync(NetworkMessage message, object? excludeClient = null);
        void StopAll();
    }
}
