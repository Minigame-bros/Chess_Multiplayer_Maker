namespace ChessServerAPI.Models
{
    public enum MessageType
    {
        JoinRequest,
        JoinResponse,
        GameState,
        Move,
        Chat,
        PlayerDisconnected,
        StartGame,
        Resign,
        OfferDraw,
        AcceptDraw,
        DeclineDraw
    }

    public class NetworkMessage
    {
        public MessageType Type { get; set; }
        public string Payload { get; set; } = string.Empty;
    }
}
