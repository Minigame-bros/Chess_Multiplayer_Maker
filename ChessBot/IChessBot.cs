using System.Threading.Tasks;
using ChessCore;

namespace ChessBot
{
    public struct BotMove
    {
        public Position From { get; set; }
        public Position To { get; set; }
        public PieceType? Promotion { get; set; }
        public PieceType? CapturedPiece { get; set; }
        public PieceType MovingPiece { get; set; }
        public bool IsCheck { get; set; }
        public bool IsCastle { get; set; }
        public int MoveNumber { get; set; }

        public BotMove(Position from, Position to, PieceType? promotion = null, PieceType? capturedPiece = null, PieceType movingPiece = PieceType.None, bool isCheck = false, bool isCastle = false, int moveNumber = 1)
        {
            From = from;
            To = to;
            Promotion = promotion;
            CapturedPiece = capturedPiece;
            MovingPiece = movingPiece;
            IsCheck = isCheck;
            IsCastle = isCastle;
            MoveNumber = moveNumber;
        }
    }

    public interface IChessBot
    {
        string Name { get; }
        string AvatarPath { get; }
        string Description { get; }
        int Elo { get; }

        Task<BotMove> CalculateMoveAsync(Game game);
        string GetSpeech(GameState state, Game? game = null, BotMove? lastMove = null);
    }

    public enum GameState
    {
        Start,
        Thinking,
        Moved,
        Checked,       // Player checked bot (legacy)
        Captured,      // Player captured bot piece (legacy)
        Won,
        Lost,
        Draw,
        BotDeliveredCheck,     // Bot delivered check to player
        BotCaptured,           // Bot captured player's piece
        Castled,               // Castling happened
        Promotion,             // Promotion happened
        DrawRefused,           // Bot refused draw offer
        PlayerResigned,        // Player surrendered
        TimeoutWon,            // Bot won by player timeout
        TimeoutLost            // Bot lost on time
    }
}
