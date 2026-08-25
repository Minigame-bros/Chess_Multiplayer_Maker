namespace ChessServerAPI.Models
{
    public class RoomInfo
    {
        public string RoomId { get; set; } = "";
        public string HostName { get; set; } = "";
        public int PlayerCount { get; set; }
    }
}
