using System.Collections.Generic;

namespace ChessCore
{
    public class Bishop : Piece
    {
        public override PieceType Type => PieceType.Bishop;
        public override PlayerColor Color { get; }

        private static readonly Direction[] dirs = new Direction[]
        {
            Direction.NorthEast,
            Direction.NorthWest,
            Direction.SouthEast,
            Direction.SouthWest
        };

        public Bishop(PlayerColor color)
        {
            Color = color;
        }

        public override Piece Copy()
        {
            Bishop copy = new Bishop(Color);
            copy.HasMoved = HasMoved;
            return copy;
        }

        public override IEnumerable<Position> GetUnhinderedMoves(Position from, Board board)
        {
            return MovePositionsInDirs(from, board, dirs);
        }
    }
}
