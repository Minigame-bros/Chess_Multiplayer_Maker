using System.Threading.Tasks;
using ChessCore;

namespace ChessBot
{
    public struct BotMove
    {
        public Position From { get; set; }
        public Position To { get; set; }
        public PieceType? Promotion { get; set; }

        public BotMove(Position from, Position to, PieceType? promotion = null)
        {
            From = from;
            To = to;
            Promotion = promotion;
        }
    }

    public interface IChessBot
    {
        string Name { get; }
        string AvatarPath { get; }
        string Description { get; }
        int Elo { get; }

        Task<BotMove> CalculateMoveAsync(Game game);
        string GetSpeech(GameState state, Game game = null, BotMove? lastMove = null);
    }

    public enum GameState
    {
        Start,
        Thinking,
        Moved,
        Checked,
        Captured,
        Won,
        Lost,
        Draw
    }
}
