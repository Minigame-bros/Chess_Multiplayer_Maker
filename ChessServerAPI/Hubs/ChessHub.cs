using Microsoft.AspNetCore.SignalR;
using ChessServerAPI.Models;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System;

namespace ChessServerAPI.Hubs
{
    public class ChessHub : Hub
    {
        // ConnectionId -> RoomName
        private static readonly ConcurrentDictionary<string, string> _userRooms = new();
        
        // RoomName -> RoomInfo
        public static readonly ConcurrentDictionary<string, RoomInfo> ActiveRooms = new();

        public async Task JoinRoom(string roomName, string playerName, bool isHost)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
            _userRooms[Context.ConnectionId] = roomName;

            ActiveRooms.AddOrUpdate(roomName, 
                new RoomInfo { RoomId = roomName, HostName = isHost ? playerName : "Unknown", PlayerCount = 1 },
                (key, existingRoom) => 
                {
                    existingRoom.PlayerCount++;
                    if (isHost) existingRoom.HostName = playerName;
                    return existingRoom;
                });
        }

        public async Task LeaveRoom(string roomName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);
            _userRooms.TryRemove(Context.ConnectionId, out _);

            if (ActiveRooms.TryGetValue(roomName, out var room))
            {
                room.PlayerCount--;
                if (room.PlayerCount <= 0)
                {
                    ActiveRooms.TryRemove(roomName, out _);
                }
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_userRooms.TryGetValue(Context.ConnectionId, out string? roomName) && roomName != null)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);
                _userRooms.TryRemove(Context.ConnectionId, out _);
                
                // Notify others in room
                var msg = new NetworkMessage { Type = MessageType.PlayerDisconnected, Payload = Context.ConnectionId };
                await Clients.Group(roomName).SendAsync("ReceiveMessage", msg, Context.ConnectionId);

                if (ActiveRooms.TryGetValue(roomName, out var room))
                {
                    room.PlayerCount--;
                    if (room.PlayerCount <= 0)
                    {
                        ActiveRooms.TryRemove(roomName, out _);
                    }
                }
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(string roomName, NetworkMessage message)
        {
            // Forward message to everyone in the room except the sender
            await Clients.OthersInGroup(roomName).SendAsync("ReceiveMessage", message, Context.ConnectionId);
        }
        
        public async Task SendMessageToUser(string targetConnectionId, NetworkMessage message)
        {
            // Used for specific targeting (like Host sending JoinResponse to a specific Client)
            await Clients.Client(targetConnectionId).SendAsync("ReceiveMessage", message, Context.ConnectionId);
        }
    }
}
