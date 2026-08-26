using System.Collections.Generic;

namespace ChessGame.Models
{
    public enum MessageType
    {
        JoinRequest,
        JoinResponse,
        Move,
        Chat,
        GameState,
        PlayerDisconnected,
        StartGame
    }

    public class NetworkMessage
    {
        public MessageType Type { get; set; }
        public string Payload { get; set; } = "";
    }

    public class JoinRequest
    {
        public string PlayerName { get; set; } = "";
        public bool IsSpectator { get; set; }
    }

    public class JoinResponse
    {
        public bool Success { get; set; }
        // 0 = White, 1 = Black, 2 = Spectator
        public int AssignedColor { get; set; } 
        public string HostName { get; set; } = "";
    }

    public class StartGamePayload
    {
        public int HostColor { get; set; }
        public int GameTimeMinutes { get; set; }
        public int TimeIncrementSeconds { get; set; }
    }

    public class MoveAction
    {
        public int FromRow { get; set; }
        public int FromCol { get; set; }
        public int ToRow { get; set; }
        public int ToCol { get; set; }
        public int PromotionPieceType { get; set; } = 5; // 5 = Queen
    }

    public class ChatMessage
    {
        public string Sender { get; set; } = "";
        public string Text { get; set; } = "";
    }

    public class UdpDiscoveryMessage
    {
        public string RoomName { get; set; } = "";
        public string IpAddress { get; set; } = "";
        public int Port { get; set; }
        public int PlayerCount { get; set; }
    }
}
