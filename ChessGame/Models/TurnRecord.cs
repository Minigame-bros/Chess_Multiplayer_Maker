namespace ChessGame.Models
{
    public class TurnRecord
    {
        public int TurnNumber { get; set; }
        public MoveRecord? WhiteMove { get; set; }
        public MoveRecord? BlackMove { get; set; }
        
        public string DisplayTurnNumber => $"{TurnNumber}.";
    }
}
